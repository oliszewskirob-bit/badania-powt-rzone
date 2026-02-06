Imports Microsoft.Data.Sqlite

Public Module RepeatEventsRepository

    Public Function Create(ev As RepeatEventCreate, createdBy As String) As Integer
        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"INSERT INTO RepeatEvents
(Modality, Device, EventType,
 PatientName, PatientId, Accession,
 FirstPartDateTime, FixRequestedByDoctor, TechFirstPart, Nurse,
 ReasonId, ReasonOtherText,
 Status, Description,
 CreatedAt, CreatedBy)
VALUES
($mod, $dev, $type,
 $pname, $pid, $acc,
 $dt1, $doc, $tech, $nurse,
 $rid, $rother,
 'new', $desc,
 $now, $by);
SELECT last_insert_rowid();"

                cmd.Parameters.AddWithValue("$mod", ev.Modality)
                cmd.Parameters.AddWithValue("$dev", ev.Device)
                cmd.Parameters.AddWithValue("$type", ev.EventType)

                cmd.Parameters.AddWithValue("$pname", ev.PatientName)
                cmd.Parameters.AddWithValue("$pid", ev.PatientId)
                cmd.Parameters.AddWithValue("$acc", If(String.IsNullOrWhiteSpace(ev.Accession), DBNull.Value, ev.Accession))

                cmd.Parameters.AddWithValue("$dt1", ev.FirstPartDateTime.ToString("o"))
                cmd.Parameters.AddWithValue("$doc", ev.FixRequestedByDoctor)
                cmd.Parameters.AddWithValue("$tech", ev.TechFirstPart)
                cmd.Parameters.AddWithValue("$nurse", ev.Nurse)

                cmd.Parameters.AddWithValue("$rid", ev.ReasonId)
                cmd.Parameters.AddWithValue("$rother", If(String.IsNullOrWhiteSpace(ev.ReasonOtherText), DBNull.Value, ev.ReasonOtherText))

                cmd.Parameters.AddWithValue("$desc", If(String.IsNullOrWhiteSpace(ev.Description), DBNull.Value, ev.Description))

                cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"))
                cmd.Parameters.AddWithValue("$by", createdBy)

                Dim newId = Convert.ToInt32(cmd.ExecuteScalar())
                Return newId
            End Using
        End Using
    End Function

    ' --- STATUS ---

    Public Sub SetStatus(eventId As Integer, newStatus As String, changedBy As String)
        newStatus = NormalizeStatus(newStatus)

        If eventId <= 0 Then Throw New Exception("Nieprawidłowe Id wpisu.")
        If String.IsNullOrWhiteSpace(changedBy) Then changedBy = "unknown"

        Using con = Db.Open()
            Using tx = con.BeginTransaction()

                ' 1) Pobierz stary status (i sprawdź czy wpis istnieje i nie jest usunięty)
                Dim oldStatus As String = ""
                Using getCmd = con.CreateCommand()
                    getCmd.Transaction = tx
                    getCmd.CommandText =
"SELECT COALESCE(Status,'new')
 FROM RepeatEvents
 WHERE Id=$id AND IFNULL(IsDeleted,0)=0;"
                    getCmd.Parameters.AddWithValue("$id", eventId)

                    Dim obj = getCmd.ExecuteScalar()
                    If obj Is Nothing OrElse obj Is DBNull.Value Then
                        Throw New Exception("Nie znaleziono wpisu (lub został usunięty).")
                    End If

                    oldStatus = obj.ToString()
                End Using

                ' jeśli bez zmian, nie rób nic (i nie loguj)
                If String.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase) Then
                    tx.Commit()
                    Exit Sub
                End If

                ' 2) Update
                Dim now = DateTime.UtcNow.ToString("o")

                Using cmd = con.CreateCommand()
                    cmd.Transaction = tx
                    cmd.CommandText =
"UPDATE RepeatEvents
 SET Status=$st,
     UpdatedAt=$ua,
     UpdatedBy=$ub,
     RowVersion = RowVersion + 1,
     ClosedAt = CASE WHEN $st='closed' THEN $ua ELSE ClosedAt END,
     ClosedBy = CASE WHEN $st='closed' THEN $ub ELSE ClosedBy END
 WHERE Id=$id
   AND IFNULL(IsDeleted,0)=0;"
                    cmd.Parameters.AddWithValue("$st", newStatus)
                    cmd.Parameters.AddWithValue("$ua", now)
                    cmd.Parameters.AddWithValue("$ub", changedBy)
                    cmd.Parameters.AddWithValue("$id", eventId)

                    Dim rows = cmd.ExecuteNonQuery()
                    If rows = 0 Then
                        Throw New Exception("Nie znaleziono wpisu (lub został usunięty).")
                    End If
                End Using

                ' 3) Audyt (w tej samej transakcji)
                AuditRepository.LogChange(eventId, "status", "RepeatEvents.Status", oldStatus, newStatus, changedBy)

                tx.Commit()
            End Using
        End Using
    End Sub


    ' --- NOTES ---

    Public Function GetNotes(eventId As Integer) As String
        If eventId <= 0 Then Return ""

        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"SELECT COALESCE(Notes,'')
 FROM RepeatEvents
 WHERE Id=$id AND IFNULL(IsDeleted,0)=0;"
                cmd.Parameters.AddWithValue("$id", eventId)

                Dim v = cmd.ExecuteScalar()
                If v Is Nothing OrElse v Is DBNull.Value Then Return ""
                Return CStr(v)
            End Using
        End Using
    End Function

    Public Sub SaveNotes(eventId As Integer, notes As String, changedBy As String)
        If eventId <= 0 Then Throw New Exception("Nieprawidłowe Id wpisu.")
        If String.IsNullOrWhiteSpace(changedBy) Then changedBy = "unknown"
        If notes Is Nothing Then notes = ""

        Using con = Db.Open()
            Using tx = con.BeginTransaction()

                ' 1) Pobierz stare notatki
                Dim oldNotes As String = ""
                Using getCmd = con.CreateCommand()
                    getCmd.Transaction = tx
                    getCmd.CommandText =
"SELECT COALESCE(Notes,'')
 FROM RepeatEvents
 WHERE Id=$id AND IFNULL(IsDeleted,0)=0;"
                    getCmd.Parameters.AddWithValue("$id", eventId)

                    Dim obj = getCmd.ExecuteScalar()
                    If obj Is Nothing OrElse obj Is DBNull.Value Then
                        Throw New Exception("Nie znaleziono wpisu (lub został usunięty).")
                    End If

                    oldNotes = obj.ToString()
                End Using

                ' jeśli bez zmian, nie rób nic
                If String.Equals(oldNotes, notes, StringComparison.Ordinal) Then
                    tx.Commit()
                    Exit Sub
                End If

                ' 2) Update
                Dim now = DateTime.UtcNow.ToString("o")

                Using cmd = con.CreateCommand()
                    cmd.Transaction = tx
                    cmd.CommandText =
"UPDATE RepeatEvents
 SET Notes=$n,
     UpdatedAt=$ua,
     UpdatedBy=$ub,
     RowVersion = RowVersion + 1
 WHERE Id=$id
   AND IFNULL(IsDeleted,0)=0;"
                    cmd.Parameters.AddWithValue("$n", notes)
                    cmd.Parameters.AddWithValue("$ua", now)
                    cmd.Parameters.AddWithValue("$ub", changedBy)
                    cmd.Parameters.AddWithValue("$id", eventId)

                    Dim rows = cmd.ExecuteNonQuery()
                    If rows = 0 Then
                        Throw New Exception("Nie znaleziono wpisu (lub został usunięty).")
                    End If
                End Using

                ' 3) Audyt (stare → nowe)
                AuditRepository.LogChange(eventId, "notes", "RepeatEvents.Notes", oldNotes, notes, changedBy)

                tx.Commit()
            End Using
        End Using
    End Sub


    ' --- SOFT DELETE ---

    Public Sub SoftDelete(eventId As Integer, session As UserSession)
        If eventId <= 0 Then Throw New Exception("Nieprawidłowe Id wpisu.")
        If session Is Nothing OrElse Not session.IsAdmin Then
            Throw New UnauthorizedAccessException("Usuwanie wpisów dozwolone tylko dla administratora.")
        End If

        Using con = Db.Open()
            Using tx = con.BeginTransaction()

                Dim now = DateTime.UtcNow.ToString("o")

                Using cmd = con.CreateCommand()
                    cmd.Transaction = tx
                    cmd.CommandText =
"UPDATE RepeatEvents
 SET IsDeleted=1,
     DeletedAt=$now,
     DeletedBy=$by,
     UpdatedAt=$now,
     UpdatedBy=$by,
     RowVersion = RowVersion + 1
 WHERE Id=$id
   AND IFNULL(IsDeleted,0)=0;"
                    cmd.Parameters.AddWithValue("$id", eventId)
                    cmd.Parameters.AddWithValue("$now", now)
                    cmd.Parameters.AddWithValue("$by", session.Username)

                    Dim rows = cmd.ExecuteNonQuery()
                    If rows = 0 Then
                        Throw New Exception("Nie znaleziono wpisu lub już został usunięty.")
                    End If
                End Using

                AuditRepository.LogChange(eventId, "delete", "RepeatEvents.IsDeleted", "0", "1", session.Username)

                tx.Commit()
            End Using
        End Using
    End Sub


    ' (Opcjonalnie) Przywracanie - tylko admin
    Public Sub Restore(eventId As Integer, session As UserSession)
        If eventId <= 0 Then Throw New Exception("Nieprawidłowe Id wpisu.")
        If session Is Nothing OrElse Not session.IsAdmin Then
            Throw New UnauthorizedAccessException("Przywracanie wpisów dozwolone tylko dla administratora.")
        End If

        Using con = Db.Open()
            Using tx = con.BeginTransaction()

                Dim now = DateTime.UtcNow.ToString("o")

                Using cmd = con.CreateCommand()
                    cmd.Transaction = tx
                    cmd.CommandText =
"UPDATE RepeatEvents
 SET IsDeleted=0,
     DeletedAt=NULL,
     DeletedBy=NULL,
     UpdatedAt=$now,
     UpdatedBy=$by,
     RowVersion = RowVersion + 1
 WHERE Id=$id
   AND IFNULL(IsDeleted,0)=1;"
                    cmd.Parameters.AddWithValue("$id", eventId)
                    cmd.Parameters.AddWithValue("$now", now)
                    cmd.Parameters.AddWithValue("$by", session.Username)

                    Dim rows = cmd.ExecuteNonQuery()
                    If rows = 0 Then
                        Throw New Exception("Nie znaleziono wpisu do przywrócenia (albo nie był usunięty).")
                    End If
                End Using

                AuditRepository.LogChange(eventId, "restore", "RepeatEvents.IsDeleted", "1", "0", session.Username)

                tx.Commit()
            End Using
        End Using
    End Sub


    ' --- HELPERS ---

    Private Function NormalizeStatus(st As String) As String
        st = If(st, "").Trim().ToLowerInvariant()
        If st = "" Then st = "new"

        If st <> "new" AndAlso st <> "in_progress" AndAlso st <> "closed" Then
            Throw New Exception("Nieprawidłowy status: " & st)
        End If

        Return st
    End Function

End Module
