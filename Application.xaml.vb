Imports System.IO
Imports System.Windows

Partial Public Class Application
    Inherits System.Windows.Application

    Public Sub New()
        InitializeComponent()

        Dim exeDir As String = AppContext.BaseDirectory
        Dim dbPath As String = Path.Combine(exeDir, "qtkr.db")

        Db.Init(dbPath)
        UserRepository.EnsureAdminUser("admin", "Administrator", "Admin123!")
    End Sub
End Class
