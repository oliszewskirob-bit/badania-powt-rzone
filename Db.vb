Imports Microsoft.Data.Sqlite
Imports System.IO

Public Module Db

    Public Property DbPath As String = ""

    Public ReadOnly Property ConnectionString As String
        Get
            Return $"Data Source={DbPath};Cache=Shared;Mode=ReadWriteCreate;"
        End Get
    End Property

    Public Sub Init(dbFilePath As String)
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
            Dim schemaSql = File.ReadAllText(schemaSqlPath)

            Using cmd = con.CreateCommand()
                cmd.CommandText = schemaSql
                cmd.ExecuteNonQuery()
            End Using
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

End Module
