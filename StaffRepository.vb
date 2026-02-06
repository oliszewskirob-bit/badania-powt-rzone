Imports Microsoft.Data.Sqlite

Public Module StaffRepository

    Public Function ListNames(staffType As String) As List(Of String)
        Dim list As New List(Of String)
        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"SELECT Name FROM StaffDirectory
 WHERE StaffType=$t AND IsActive=1
 ORDER BY Name;"
                cmd.Parameters.AddWithValue("$t", staffType)
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        list.Add(r.GetString(0))
                    End While
                End Using
            End Using
        End Using
        Return list
    End Function

    Public Sub EnsureExists(staffType As String, name As String)
        name = If(name, "").Trim()
        If name = "" Then Return

        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"INSERT INTO StaffDirectory(StaffType, Name, IsActive)
SELECT $t, $n, 1
WHERE NOT EXISTS (
  SELECT 1 FROM StaffDirectory WHERE StaffType=$t AND Name=$n
);"
                cmd.Parameters.AddWithValue("$t", staffType)
                cmd.Parameters.AddWithValue("$n", name)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

End Module
