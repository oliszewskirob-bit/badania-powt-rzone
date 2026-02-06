Imports Microsoft.Data.Sqlite

Public Class AttachmentRow
    Public Property Id As Integer
    Public Property EventId As Integer
    Public Property FileName As String
    Public Property StoredPath As String
    Public Property AddedAt As String
    Public Property AddedBy As String
End Class

Public Module AttachmentsRepository

    Public Function ListForEvent(eventId As Integer) As List(Of AttachmentRow)
        Dim list As New List(Of AttachmentRow)
        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"SELECT Id, EventId, FileName, StoredPath, AddedAt, AddedBy
 FROM EventAttachments
 WHERE EventId=$id
 ORDER BY Id DESC;"
                cmd.Parameters.AddWithValue("$id", eventId)
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        list.Add(New AttachmentRow With {
                            .Id = r.GetInt32(0),
                            .EventId = r.GetInt32(1),
                            .FileName = r.GetString(2),
                            .StoredPath = r.GetString(3),
                            .AddedAt = r.GetString(4),
                            .AddedBy = r.GetString(5)
                        })
                    End While
                End Using
            End Using
        End Using
        Return list
    End Function

    Public Sub AddAttachment(eventId As Integer, sourceFilePath As String, addedBy As String)
        Dim baseFolder = AppDomain.CurrentDomain.BaseDirectory
        Dim destFolder = System.IO.Path.Combine(baseFolder, "Attachments", eventId.ToString())
        System.IO.Directory.CreateDirectory(destFolder)

        Dim fileName = System.IO.Path.GetFileName(sourceFilePath)
        Dim destPath = System.IO.Path.Combine(destFolder, fileName)

        ' Jeśli istnieje, dopisz suffix
        If System.IO.File.Exists(destPath) Then
            Dim nameOnly = System.IO.Path.GetFileNameWithoutExtension(fileName)
            Dim ext = System.IO.Path.GetExtension(fileName)
            destPath = System.IO.Path.Combine(destFolder, nameOnly & "_" & DateTime.Now.ToString("yyyyMMdd_HHmmss") & ext)
            fileName = System.IO.Path.GetFileName(destPath)
        End If

        System.IO.File.Copy(sourceFilePath, destPath, False)

        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"INSERT INTO EventAttachments(EventId, FileName, StoredPath, AddedAt, AddedBy)
 VALUES ($e, $fn, $p, $at, $ab);"
                cmd.Parameters.AddWithValue("$e", eventId)
                cmd.Parameters.AddWithValue("$fn", fileName)
                cmd.Parameters.AddWithValue("$p", destPath)
                cmd.Parameters.AddWithValue("$at", DateTime.UtcNow.ToString("o"))
                cmd.Parameters.AddWithValue("$ab", addedBy)
                cmd.ExecuteNonQuery()
            End Using
        End Using

        AuditRepository.LogChange(eventId, "attachment_add", "EventAttachments.FileName", "", fileName, addedBy)
    End Sub

End Module
