Public Module UiLabelHelpers

    Public Function StatusLabel(status As String) As String
        Select Case (If(status, "").Trim().ToLowerInvariant())
            Case "new"
                Return "Nowe"
            Case "in_progress"
                Return "W trakcie"
            Case "closed"
                Return "Zamknięte"
            Case Else
                Return status
        End Select
    End Function

    Public Function EventTypeLabel(eventType As String) As String
        Select Case (If(eventType, "").Trim().ToLowerInvariant())
            Case "repeat"
                Return "Powtórzenie"
            Case "supplement"
                Return "Uzupełnienie"
            Case "contrast_extravasation"
                Return "Wynaczynienie kontrastu"
            Case Else
                Return eventType
        End Select
    End Function

    Public Function YesNoLabel(value As Boolean?) As String
        If Not value.HasValue Then
            Return ""
        End If
        Return If(value.Value, "Tak", "Nie")
    End Function

End Module
