Imports System.Globalization

Public Module DateTimeHelpers

    Public Function ParseDateTimeOrMin(value As String, Optional convertToLocal As Boolean = True) As DateTime
        If String.IsNullOrWhiteSpace(value) Then
            Return DateTime.MinValue
        End If

        Dim parsed As DateTime
        If DateTime.TryParse(value, Nothing, DateTimeStyles.RoundtripKind, parsed) Then
            Return If(convertToLocal, parsed.ToLocalTime(), parsed)
        End If

        If DateTime.TryParse(value, Nothing, DateTimeStyles.AssumeUniversal Or DateTimeStyles.AdjustToUniversal, parsed) Then
            Return If(convertToLocal, parsed.ToLocalTime(), parsed)
        End If

        Return DateTime.MinValue
    End Function

End Module
