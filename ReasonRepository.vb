Imports Microsoft.Data.Sqlite

Public Module ReasonRepository

    Public Function GetReasons(modality As String) As List(Of ReasonItem)
        Dim result As New List(Of ReasonItem)

        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"SELECT Id, Name
FROM Reasons
WHERE Modality = $m AND IsActive = 1
ORDER BY Name;"
                cmd.Parameters.AddWithValue("$m", modality)

                Using r = cmd.ExecuteReader()
                    While r.Read()
                        result.Add(New ReasonItem With {
                            .Id = r.GetInt32(0),
                            .Name = r.GetString(1)
                        })
                    End While
                End Using
            End Using
        End Using

        Return result
    End Function

End Module
