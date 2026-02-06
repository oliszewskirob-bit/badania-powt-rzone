Imports System.Security.Cryptography
Imports System.Text

Public Module PasswordHasher

    ' Parametry PBKDF2 – możesz zmienić, ale zostaw tak na start
    Private Const Iterations As Integer = 120000
    Private Const SaltSize As Integer = 16
    Private Const HashSize As Integer = 32 ' 256-bit

    Public Function HashPassword(password As String) As (Hash As Byte(), Salt As Byte())
        If password Is Nothing Then password = ""

        ' .NET 10: RandomNumberGenerator static APIs (bez RNGCryptoServiceProvider)
        Dim salt(SaltSize - 1) As Byte
        RandomNumberGenerator.Fill(salt)

        ' .NET 10: Rfc2898DeriveBytes.Pbkdf2 static (bez przestarzałych ctorów)
        Dim hash As Byte() = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize
        )

        Return (hash, salt)
    End Function

    Public Function VerifyPassword(password As String, expectedHash As Byte(), salt As Byte()) As Boolean
        If password Is Nothing Then password = ""
        If expectedHash Is Nothing OrElse salt Is Nothing Then Return False

        Dim computed As Byte() = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            expectedHash.Length
        )

        ' stałoczasowe porównanie
        Return CryptographicOperations.FixedTimeEquals(computed, expectedHash)
    End Function

End Module
