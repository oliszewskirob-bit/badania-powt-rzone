Imports Microsoft.Data.Sqlite
Imports System.IO
Imports System.Linq

Public Module Db

    Public Property DbPath As String = ""

    Public ReadOnly Property ConnectionString As String
        Get
            Return $"Data Source={DbPath};Cache=Shared;Mode=ReadWriteCreate;"
        End Get
    End Property

    Public Sub Init(dbFilePath As String)
        If String.IsNullOrWhiteSpace(dbFilePath) Then
            Throw New ArgumentException("Ścieżka do pliku bazy danych nie może być pusta.", NameOf(dbFilePath))
        End If

        DbPath = dbFilePath

        Dim dir = Path.GetDirectoryName(DbPath)
        If Not String.IsNullOrWhiteSpace(dir) AndAlso Not Directory.Exists(dir) Then
            Directory.CreateDirectory(dir)
        End If

        Using con As New SqliteConnection(ConnectionString)
            con.Open()

            Using cmd = con.CreateCommand()
                cmd.CommandText =
"PRAGMA journal_mode=WAL;
PRAGMA synchronous=NORMAL;
PRAGMA foreign_keys=ON;
PRAGMA busy_timeout=5000;"
                cmd.ExecuteNonQuery()
            End Using

            Dim schemaSqlPath = Path.Combine(AppContext.BaseDirectory, "Data", "Schema.sql")
            If Not File.Exists(schemaSqlPath) Then
                Dim fallbackPath = Path.Combine(AppContext.BaseDirectory, "Schema.sql")
                If File.Exists(fallbackPath) Then
                    schemaSqlPath = fallbackPath
                End If
            End If

            If Not File.Exists(schemaSqlPath) Then
                Throw New FileNotFoundException("Nie znaleziono pliku Schema.sql potrzebnego do inicjalizacji bazy.", schemaSqlPath)
            End If

            Dim schemaSql = File.ReadAllText(schemaSqlPath)

            Using cmd = con.CreateCommand()
                cmd.CommandText = schemaSql
                cmd.ExecuteNonQuery()
            End Using

            EnsureSchema(con)
        End Using
    End Sub

    Public Function Open() As SqliteConnection
        Dim con As New SqliteConnection(ConnectionString)
        con.Open()
        Using cmd = con.CreateCommand()
            cmd.CommandText = "PRAGMA foreign_keys=ON; PRAGMA busy_timeout=5000;"
            cmd.ExecuteNonQuery()
        End Using
        Return con
    End Function

    Private Sub EnsureSchema(con As SqliteConnection)
        EnsureUserSchema(con)
        EnsureRepeatEventsSchema(con)
        EnsureEventAttachmentsSchema(con)
        EnsureAuditLogSchema(con)
        EnsureReasonSeeds(con)
    End Sub

    Private Sub EnsureUserSchema(con As SqliteConnection)
        EnsureColumn(con, "Users", "MustChangePassword", "INTEGER NOT NULL DEFAULT 0")
        EnsureColumn(con, "Users", "UpdatedAt", "TEXT NULL")
        EnsureColumn(con, "Users", "UpdatedBy", "TEXT NULL")
    End Sub

    Private Sub EnsureRepeatEventsSchema(con As SqliteConnection)
        If RepeatEventsNeedsMigration(con) Then
            MigrateRepeatEventsTable(con)
        End If

        EnsureColumn(con, "RepeatEvents", "ReasonOtherText", "TEXT NULL")
        EnsureColumn(con, "RepeatEvents", "FixDateTime", "TEXT NULL")
        EnsureColumn(con, "RepeatEvents", "ExtraMinutes", "INTEGER NULL")
        EnsureColumn(con, "RepeatEvents", "Outcome", "TEXT NULL")
        EnsureColumn(con, "RepeatEvents", "Description", "TEXT NULL")
        EnsureColumn(con, "RepeatEvents", "CorrectiveAction", "TEXT NULL")
        EnsureColumn(con, "RepeatEvents", "Notes", "TEXT NULL")
        EnsureColumn(con, "RepeatEvents", "IsContrastExtravasation", "INTEGER NOT NULL DEFAULT 0")
        EnsureColumn(con, "RepeatEvents", "ContrastCannula", "TEXT NULL")
        EnsureColumn(con, "RepeatEvents", "ContrastType", "TEXT NULL")
        EnsureColumn(con, "RepeatEvents", "ContrastFlow", "TEXT NULL")
        EnsureColumn(con, "RepeatEvents", "ContrastVolume", "TEXT NULL")
        EnsureColumn(con, "RepeatEvents", "ContrastVisible", "INTEGER NULL")
        EnsureColumn(con, "RepeatEvents", "WardNotified", "INTEGER NULL")
        EnsureColumn(con, "RepeatEvents", "PatientInstructions", "INTEGER NULL")
        EnsureColumn(con, "RepeatEvents", "ContrastAdditionalInfo", "TEXT NULL")
        EnsureColumn(con, "RepeatEvents", "UpdatedAt", "TEXT NULL")
        EnsureColumn(con, "RepeatEvents", "UpdatedBy", "TEXT NULL")
        EnsureColumn(con, "RepeatEvents", "ClosedAt", "TEXT NULL")
        EnsureColumn(con, "RepeatEvents", "ClosedBy", "TEXT NULL")
        EnsureColumn(con, "RepeatEvents", "RowVersion", "INTEGER NOT NULL DEFAULT 1")
        EnsureColumn(con, "RepeatEvents", "IsDeleted", "INTEGER NOT NULL DEFAULT 0")
        EnsureColumn(con, "RepeatEvents", "DeletedAt", "TEXT NULL")
        EnsureColumn(con, "RepeatEvents", "DeletedBy", "TEXT NULL")
    End Sub

    Private Sub EnsureReasonSeeds(con As SqliteConnection)
        EnsureReasonSeed(con, "CT", "Wynaczynienie kontrastu")
        EnsureReasonSeed(con, "MR", "Wynaczynienie kontrastu")
    End Sub

    Private Sub EnsureReasonSeed(con As SqliteConnection, modality As String, name As String)
        Using cmd = con.CreateCommand()
            cmd.CommandText =
"INSERT INTO Reasons (Modality, Name, IsActive)
SELECT $m, $n, 1
WHERE NOT EXISTS (
  SELECT 1 FROM Reasons WHERE Modality=$m AND Name=$n
);"
            cmd.Parameters.AddWithValue("$m", modality)
            cmd.Parameters.AddWithValue("$n", name)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    Private Sub EnsureEventAttachmentsSchema(con As SqliteConnection)
        If Not TableExists(con, "EventAttachments") Then
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"CREATE TABLE IF NOT EXISTS EventAttachments (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  EventId INTEGER NOT NULL,
  FileName TEXT NOT NULL,
  StoredPath TEXT NOT NULL,
  AddedAt TEXT NOT NULL,
  AddedBy TEXT NOT NULL,
  FOREIGN KEY(EventId) REFERENCES RepeatEvents(Id)
);"
                cmd.ExecuteNonQuery()
            End Using
        End If
    End Sub

    Private Sub EnsureAuditLogSchema(con As SqliteConnection)
        If Not TableExists(con, "AuditLog") Then
            CreateAuditLogTable(con)
            Return
        End If
        Dim tableSql = GetTableSql(con, "AuditLog")

        If TableHasColumn(con, "AuditLog", "EntityName") OrElse TableHasColumn(con, "AuditLog", "ChangesJson") Then
            Dim legacyName = $"AuditLog_Legacy_{DateTime.UtcNow:yyyyMMddHHmmss}"
            RenameTable(con, "AuditLog", legacyName)
            MigrateAuditLogTable(con, legacyName)
            Return
        End If

        If tableSql.Contains("EventId INTEGER NOT NULL", StringComparison.OrdinalIgnoreCase) Then
            Dim legacyName = $"AuditLog_Legacy_{DateTime.UtcNow:yyyyMMddHHmmss}"
            RenameTable(con, "AuditLog", legacyName)
            MigrateAuditLogTable(con, legacyName)
            Return
        End If

        EnsureColumn(con, "AuditLog", "EventId", "INTEGER NULL")
        EnsureColumn(con, "AuditLog", "Action", "TEXT NOT NULL DEFAULT ''")
        EnsureColumn(con, "AuditLog", "FieldName", "TEXT NOT NULL DEFAULT ''")
        EnsureColumn(con, "AuditLog", "OldValue", "TEXT NOT NULL DEFAULT ''")
        EnsureColumn(con, "AuditLog", "NewValue", "TEXT NOT NULL DEFAULT ''")
        EnsureColumn(con, "AuditLog", "ChangedAt", "TEXT NOT NULL DEFAULT ''")
        EnsureColumn(con, "AuditLog", "ChangedBy", "TEXT NOT NULL DEFAULT ''")
        EnsureColumn(con, "AuditLog", "Machine", "TEXT NOT NULL DEFAULT ''")
        EnsureColumn(con, "AuditLog", "AppVersion", "TEXT NOT NULL DEFAULT ''")
    End Sub

    Private Sub CreateAuditLogTable(con As SqliteConnection)
        Using cmd = con.CreateCommand()
            cmd.CommandText =
"CREATE TABLE IF NOT EXISTS AuditLog (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  EventId INTEGER NULL,
  Action TEXT NOT NULL,
  FieldName TEXT NOT NULL,
  OldValue TEXT NOT NULL,
  NewValue TEXT NOT NULL,
  ChangedAt TEXT NOT NULL,
  ChangedBy TEXT NOT NULL,
  Machine TEXT NOT NULL,
  AppVersion TEXT NOT NULL,
  FOREIGN KEY(EventId) REFERENCES RepeatEvents(Id)
);"
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    Private Sub RenameTable(con As SqliteConnection, fromName As String, toName As String)
        Using cmd = con.CreateCommand()
            cmd.CommandText = $"ALTER TABLE {fromName} RENAME TO {toName};"
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    Private Sub MigrateAuditLogTable(con As SqliteConnection, legacyName As String)
        CreateAuditLogTable(con)

        Dim newColumns As New List(Of String) From {
            "Id", "EventId", "Action", "FieldName", "OldValue", "NewValue",
            "ChangedAt", "ChangedBy", "Machine", "AppVersion"
        }

        Dim legacyColumns = GetTableColumns(con, legacyName)
        Dim columnsToCopy = newColumns.Where(Function(c) legacyColumns.Contains(c)).ToList()

        If columnsToCopy.Count = 0 Then
            Return
        End If

        Dim columnList = String.Join(", ", columnsToCopy)
        Using cmd = con.CreateCommand()
            cmd.CommandText = $"INSERT INTO AuditLog ({columnList}) SELECT {columnList} FROM {legacyName};"
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    Private Function TableExists(con As SqliteConnection, tableName As String) As Boolean
        Using cmd = con.CreateCommand()
            cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name;"
            cmd.Parameters.AddWithValue("$name", tableName)
            Dim result = cmd.ExecuteScalar()
            Return result IsNot Nothing
        End Using
    End Function

    Private Function GetTableSql(con As SqliteConnection, tableName As String) As String
        Using cmd = con.CreateCommand()
            cmd.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name=$name;"
            cmd.Parameters.AddWithValue("$name", tableName)
            Dim result = cmd.ExecuteScalar()
            If result Is Nothing OrElse result Is DBNull.Value Then
                Return ""
            End If
            Return result.ToString()
        End Using
    End Function

    Private Function GetTableColumns(con As SqliteConnection, tableName As String) As HashSet(Of String)
        Dim cols As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Using cmd = con.CreateCommand()
            cmd.CommandText = $"PRAGMA table_info({tableName});"
            Using r = cmd.ExecuteReader()
                While r.Read()
                    cols.Add(r.GetString(1))
                End While
            End Using
        End Using
        Return cols
    End Function

    Private Function RepeatEventsNeedsMigration(con As SqliteConnection) As Boolean
        If Not TableExists(con, "RepeatEvents") Then
            Return False
        End If

        Dim tableSql = GetTableSql(con, "RepeatEvents")
        If String.IsNullOrWhiteSpace(tableSql) Then
            Return False
        End If

        Return Not tableSql.Contains("contrast_extravasation", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Sub MigrateRepeatEventsTable(con As SqliteConnection)
        Using tx = con.BeginTransaction()
            Using pragmaCmd = con.CreateCommand()
                pragmaCmd.Transaction = tx
                pragmaCmd.CommandText = "PRAGMA foreign_keys=OFF;"
                pragmaCmd.ExecuteNonQuery()
            End Using

            Using cmd = con.CreateCommand()
                cmd.Transaction = tx
                cmd.CommandText =
"CREATE TABLE IF NOT EXISTS RepeatEvents_New (
  Id INTEGER PRIMARY KEY AUTOINCREMENT,
  Modality TEXT NOT NULL CHECK(Modality IN ('CT','MR')),
  Device TEXT NOT NULL,
  EventType TEXT NOT NULL CHECK(EventType IN ('repeat','supplement','contrast_extravasation')),
  PatientName TEXT NOT NULL,
  PatientId TEXT NOT NULL,
  Accession TEXT NULL,
  FirstPartDateTime TEXT NOT NULL,
  FixRequestedByDoctor TEXT NOT NULL,
  TechFirstPart TEXT NOT NULL,
  Nurse TEXT NOT NULL,
  ReasonId INTEGER NOT NULL,
  ReasonOtherText TEXT NULL,
  FixDateTime TEXT NULL,
  ExtraMinutes INTEGER NULL,
  Status TEXT NOT NULL CHECK(Status IN ('new','in_progress','closed')) DEFAULT 'new',
  Outcome TEXT NULL,
  Description TEXT NULL,
  CorrectiveAction TEXT NULL,
  Notes TEXT NULL,
  IsContrastExtravasation INTEGER NOT NULL DEFAULT 0,
  ContrastCannula TEXT NULL,
  ContrastType TEXT NULL,
  ContrastFlow TEXT NULL,
  ContrastVolume TEXT NULL,
  ContrastVisible INTEGER NULL,
  WardNotified INTEGER NULL,
  PatientInstructions INTEGER NULL,
  ContrastAdditionalInfo TEXT NULL,
  CreatedAt TEXT NOT NULL,
  CreatedBy TEXT NOT NULL,
  UpdatedAt TEXT NULL,
  UpdatedBy TEXT NULL,
  ClosedAt TEXT NULL,
  ClosedBy TEXT NULL,
  RowVersion INTEGER NOT NULL DEFAULT 1,
  IsDeleted INTEGER NOT NULL DEFAULT 0,
  DeletedAt TEXT NULL,
  DeletedBy TEXT NULL,
  FOREIGN KEY(ReasonId) REFERENCES Reasons(Id)
);"
                cmd.ExecuteNonQuery()
            End Using

            Dim newColumns As New List(Of String) From {
                "Id", "Modality", "Device", "EventType", "PatientName", "PatientId", "Accession",
                "FirstPartDateTime", "FixRequestedByDoctor", "TechFirstPart", "Nurse", "ReasonId",
                "ReasonOtherText", "FixDateTime", "ExtraMinutes", "Status", "Outcome", "Description",
                "CorrectiveAction", "Notes", "IsContrastExtravasation", "ContrastCannula",
                "ContrastType", "ContrastFlow", "ContrastVolume", "ContrastVisible", "WardNotified",
                "PatientInstructions", "ContrastAdditionalInfo", "CreatedAt", "CreatedBy", "UpdatedAt",
                "UpdatedBy", "ClosedAt", "ClosedBy", "RowVersion", "IsDeleted", "DeletedAt", "DeletedBy"
            }

            Dim existingColumns = GetTableColumns(con, "RepeatEvents")
            Dim columnsToCopy = newColumns.Where(Function(c) existingColumns.Contains(c)).ToList()

            If columnsToCopy.Count > 0 Then
                Dim columnList = String.Join(", ", columnsToCopy)
                Using insertCmd = con.CreateCommand()
                    insertCmd.Transaction = tx
                    insertCmd.CommandText = $"INSERT INTO RepeatEvents_New ({columnList}) SELECT {columnList} FROM RepeatEvents;"
                    insertCmd.ExecuteNonQuery()
                End Using
            End If

            Using dropCmd = con.CreateCommand()
                dropCmd.Transaction = tx
                dropCmd.CommandText = "DROP TABLE RepeatEvents;"
                dropCmd.ExecuteNonQuery()
            End Using

            Using renameCmd = con.CreateCommand()
                renameCmd.Transaction = tx
                renameCmd.CommandText = "ALTER TABLE RepeatEvents_New RENAME TO RepeatEvents;"
                renameCmd.ExecuteNonQuery()
            End Using

            Using pragmaOnCmd = con.CreateCommand()
                pragmaOnCmd.Transaction = tx
                pragmaOnCmd.CommandText = "PRAGMA foreign_keys=ON;"
                pragmaOnCmd.ExecuteNonQuery()
            End Using

            tx.Commit()
        End Using
    End Sub

    Private Function TableHasColumn(con As SqliteConnection, tableName As String, columnName As String) As Boolean
        Using cmd = con.CreateCommand()
            cmd.CommandText = $"PRAGMA table_info({tableName});"
            Using r = cmd.ExecuteReader()
                While r.Read()
                    If String.Equals(r.GetString(1), columnName, StringComparison.OrdinalIgnoreCase) Then
                        Return True
                    End If
                End While
            End Using
        End Using
        Return False
    End Function

    Private Sub EnsureColumn(con As SqliteConnection, tableName As String, columnName As String, columnDefinition As String)
        If Not TableExists(con, tableName) Then
            Return
        End If

        If TableHasColumn(con, tableName, columnName) Then
            Return
        End If

        Using cmd = con.CreateCommand()
            cmd.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};"
            cmd.ExecuteNonQuery()
        End Using
    End Sub

End Module
