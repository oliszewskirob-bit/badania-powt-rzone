Imports System.IO
Imports System.Text

Public Module CsvExport

    Private Function EscapeCsv(value As String) As String
        If value Is Nothing Then value = ""
        Dim mustQuote = value.Contains(",") OrElse value.Contains("""") OrElse value.Contains(vbCr) OrElse value.Contains(vbLf)
        value = value.Replace("""", """""")
        If mustQuote Then value = $"""{value}"""
        Return value
    End Function

    Public Sub WriteCsv(path As String, headers As List(Of String), rows As List(Of Dictionary(Of String, String)))
        Using sw As New StreamWriter(path, False, New UTF8Encoding(True))
            sw.WriteLine(String.Join(",", headers.Select(Function(h) EscapeCsv(h))))

            For Each row In rows
                Dim line = headers.Select(Function(h)
                                              Dim v As String = ""
                                              If row.ContainsKey(h) Then v = row(h)
                                              Return EscapeCsv(v)
                                          End Function)
                sw.WriteLine(String.Join(",", line))
            Next
        End Using
    End Sub

End Module
