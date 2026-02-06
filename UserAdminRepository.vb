Imports Microsoft.Data.Sqlite

Public Module UserAdminRepository

    Public Function ListUsers() As List(Of UserRow)
        Dim list As New List(Of UserRow)

        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"SELECT Id, Username, DisplayName, Role, IsActive, CreatedAt, CreatedBy
 FROM Users
 ORDER BY Username;"
                Using r = cmd.ExecuteReader()
                    While r.Read()
                        list.Add(New UserRow With {
                            .Id = r.GetInt32(0),
                            .Username = r.GetString(1),
                            .DisplayName = r.GetString(2),
                            .Role = r.GetString(3),
                            .IsActive = (r.GetInt32(4) = 1),
                            .CreatedAt = r.GetString(5),
                            .CreatedBy = r.GetString(6)
                        })
                    End While
                End Using
            End Using
        End Using

        Return list
    End Function

    Public Sub CreateUser(username As String, displayName As String, role As String, passwordPlain As String, createdBy As String)
        username = (If(username, "")).Trim().ToLowerInvariant()
        displayName = (If(displayName, "")).Trim()
        role = (If(role, "user")).Trim().ToLowerInvariant()

        If username = "" Then Throw New Exception("Username nie może być pusty.")
        If displayName = "" Then Throw New Exception("DisplayName nie może być pusty.")
        If role <> "admin" AndAlso role <> "user" Then Throw New Exception("Rola musi być admin lub user.")
        If (If(passwordPlain, "")).Length < 6 Then Throw New Exception("Hasło musi mieć min. 6 znaków.")

        Dim hp = PasswordHasher.HashPassword(passwordPlain) ' zwraca (Hash As Byte(), Salt As Byte())

        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"INSERT INTO Users
 (Username, DisplayName, Role, PasswordHash, PasswordSalt,
  IsActive, MustChangePassword, CreatedAt, CreatedBy)
 VALUES
 ($u, $dn, $r, $h, $s,
  1, 1, $ca, $cb);"


                cmd.Parameters.AddWithValue("$u", username)
                cmd.Parameters.AddWithValue("$dn", displayName)
                cmd.Parameters.AddWithValue("$r", role)

                ' KLUCZ: zapis jako BLOB, nie Base64 TEXT
                cmd.Parameters.Add("$h", SqliteType.Blob).Value = hp.Hash
                cmd.Parameters.Add("$s", SqliteType.Blob).Value = hp.Salt

                cmd.Parameters.AddWithValue("$ca", DateTime.UtcNow.ToString("o"))
                cmd.Parameters.AddWithValue("$cb", createdBy)

                cmd.ExecuteNonQuery()
            End Using
        End Using

        AuditRepository.LogChange(0, "user_create", "Users.Username", "", username, createdBy)
    End Sub

    Public Sub ResetPassword(userId As Integer, newPasswordPlain As String, changedBy As String)
        If (If(newPasswordPlain, "")).Length < 6 Then Throw New Exception("Hasło musi mieć min. 6 znaków.")

        Dim hp = PasswordHasher.HashPassword(newPasswordPlain)

        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"UPDATE Users
 SET PasswordHash=$h,
     PasswordSalt=$s,
     UpdatedAt=$ua,
     UpdatedBy=$ub
 WHERE Id=$id;"

                cmd.Parameters.Add("$h", SqliteType.Blob).Value = hp.Hash
                cmd.Parameters.Add("$s", SqliteType.Blob).Value = hp.Salt
                cmd.Parameters.AddWithValue("$ua", DateTime.UtcNow.ToString("o"))
                cmd.Parameters.AddWithValue("$ub", changedBy)
                cmd.Parameters.AddWithValue("$id", userId)

                cmd.ExecuteNonQuery()
            End Using
        End Using

        AuditRepository.LogChange(0, "user_resetpwd", "Users.Id", "", userId.ToString(), changedBy)
    End Sub

    Public Sub SetActive(userId As Integer, isActive As Boolean, changedBy As String)
        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"UPDATE Users
 SET IsActive=$a,
     UpdatedAt=$ua,
     UpdatedBy=$ub
 WHERE Id=$id;"

                cmd.Parameters.AddWithValue("$a", If(isActive, 1, 0))
                cmd.Parameters.AddWithValue("$ua", DateTime.UtcNow.ToString("o"))
                cmd.Parameters.AddWithValue("$ub", changedBy)
                cmd.Parameters.AddWithValue("$id", userId)
                cmd.ExecuteNonQuery()
            End Using
        End Using

        AuditRepository.LogChange(0, "user_active", "Users.Id", "", $"{userId}:{isActive}", changedBy)
    End Sub

End Module



Public Module UserPasswordRepository

    Public Sub ChangeOwnPassword(userId As Integer, newPasswordPlain As String, changedBy As String)
        If userId <= 0 Then Throw New Exception("Nieprawidłowe Id użytkownika.")
        If (If(newPasswordPlain, "")).Length < 6 Then Throw New Exception("Hasło musi mieć min. 6 znaków.")
        If String.IsNullOrWhiteSpace(changedBy) Then changedBy = "unknown"

        Dim hp = PasswordHasher.HashPassword(newPasswordPlain)

        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"UPDATE Users
 SET PasswordHash=$h,
     PasswordSalt=$s,
     MustChangePassword=0,
     UpdatedAt=$ua,
     UpdatedBy=$ub
 WHERE Id=$id
   AND IsActive=1;"

                cmd.Parameters.Add("$h", SqliteType.Blob).Value = hp.Hash
                cmd.Parameters.Add("$s", SqliteType.Blob).Value = hp.Salt
                cmd.Parameters.AddWithValue("$ua", DateTime.UtcNow.ToString("o"))
                cmd.Parameters.AddWithValue("$ub", changedBy)
                cmd.Parameters.AddWithValue("$id", userId)

                Dim rows = cmd.ExecuteNonQuery()
                If rows = 0 Then Throw New Exception("Nie znaleziono użytkownika lub jest nieaktywny.")
            End Using
        End Using

        AuditRepository.LogChange(0, "user_change_password", "Users.Id", "", userId.ToString(), changedBy)
    End Sub

End Module


Public Class UserRow
    Public Property Id As Integer
    Public Property Username As String
    Public Property DisplayName As String
    Public Property Role As String
    Public Property IsActive As Boolean
    Public Property CreatedAt As String
    Public Property CreatedBy As String
End Class
