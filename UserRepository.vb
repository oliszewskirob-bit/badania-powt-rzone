Imports Microsoft.Data.Sqlite

Public Class UserSession
    Public Property Id As Integer
    Public Property Username As String
    Public Property DisplayName As String
    Public Property Role As String
    Public Property MustChangePassword As Boolean

    Public Sub New(id As Integer, username As String, displayName As String, role As String, mustChange As Boolean)
        Me.Id = id
        Me.Username = username
        Me.DisplayName = displayName
        Me.Role = role
        Me.MustChangePassword = mustChange
    End Sub

    Public ReadOnly Property IsAdmin As Boolean
        Get
            Return String.Equals(Role, "admin", StringComparison.OrdinalIgnoreCase)
        End Get
    End Property
End Class

Public Module UserRepository

    ' Tworzy admina tylko jeśli tabela Users jest pusta
    ' i ustawia MustChangePassword=1, żeby admin od razu zmienił hasło na swoje
    Public Sub EnsureAdminUser(username As String, displayName As String, password As String)
        Using con = Db.Open()

            Using check = con.CreateCommand()
                check.CommandText = "SELECT COUNT(1) FROM Users;"
                Dim count = CInt(check.ExecuteScalar())
                If count > 0 Then Return
            End Using

            Dim hs = PasswordHasher.HashPassword(password)

            Using cmd = con.CreateCommand()
                cmd.CommandText =
"INSERT INTO Users
 (Username, DisplayName, Role, PasswordHash, PasswordSalt, MustChangePassword, IsActive, CreatedAt, CreatedBy)
 VALUES
 ($u, $d, 'admin', $h, $s, 1, 1, $now, 'system');"

                cmd.Parameters.AddWithValue("$u", username.Trim().ToLowerInvariant())
                cmd.Parameters.AddWithValue("$d", displayName.Trim())
                cmd.Parameters.Add("$h", SqliteType.Blob).Value = hs.Hash
                cmd.Parameters.Add("$s", SqliteType.Blob).Value = hs.Salt
                cmd.Parameters.AddWithValue("$now", DateTime.UtcNow.ToString("o"))

                cmd.ExecuteNonQuery()
            End Using

        End Using
    End Sub

    ' POPRAWNE logowanie: najpierw wczytaj hash/salt, potem VerifyPassword, dopiero potem Return session
    Public Function Login(username As String, password As String) As UserSession
        username = (If(username, "")).Trim().ToLowerInvariant()
        password = If(password, "")

        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"SELECT Id, Username, DisplayName, Role, PasswordHash, PasswordSalt, COALESCE(MustChangePassword,0)
 FROM Users
 WHERE lower(Username)=$u AND IsActive=1
 LIMIT 1;"

                cmd.Parameters.AddWithValue("$u", username)

                Using r = cmd.ExecuteReader()
                    If Not r.Read() Then
                        Return Nothing
                    End If

                    Dim id = r.GetInt32(0)
                    Dim un = r.GetString(1)
                    Dim dn = r.GetString(2)
                    Dim role = r.GetString(3)

                    Dim hash = CType(r("PasswordHash"), Byte())
                    Dim salt = CType(r("PasswordSalt"), Byte())
                    Dim mustChange = (Convert.ToInt32(r.GetValue(6)) = 1)

                    If Not PasswordHasher.VerifyPassword(password, hash, salt) Then
                        Return Nothing
                    End If

                    Return New UserSession(id, un, dn, role, mustChange)
                End Using
            End Using
        End Using
    End Function

    ' Zmiana hasła dla zalogowanego użytkownika + zbij MustChangePassword na 0
    Public Sub ChangePassword(userId As Integer, newPasswordPlain As String, changedBy As String)
        If userId <= 0 Then Throw New Exception("Nieprawidłowy użytkownik.")
        If (If(newPasswordPlain, "")).Length < 6 Then Throw New Exception("Hasło musi mieć min. 6 znaków.")
        If String.IsNullOrWhiteSpace(changedBy) Then changedBy = "unknown"

        Dim hp = PasswordHasher.HashPassword(newPasswordPlain)

        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"UPDATE Users
 SET PasswordHash=$h,
     PasswordSalt=$s,
     MustChangePassword=0
 WHERE Id=$id;"

                cmd.Parameters.Add("$h", SqliteType.Blob).Value = hp.Hash
                cmd.Parameters.Add("$s", SqliteType.Blob).Value = hp.Salt
                cmd.Parameters.AddWithValue("$id", userId)

                Dim rows = cmd.ExecuteNonQuery()
                If rows = 0 Then Throw New Exception("Nie znaleziono użytkownika.")
            End Using
        End Using

        AuditRepository.LogChange(0, "user_change_password", "Users.Id", "", userId.ToString(), changedBy)
    End Sub

End Module

