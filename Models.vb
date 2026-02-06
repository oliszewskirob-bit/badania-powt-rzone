Public Class ReasonItem
    Public Property Id As Integer
    Public Property Name As String
    Public Overrides Function ToString() As String
        Return Name
    End Function
End Class

Public Class RepeatEventCreate
    Public Property Modality As String            ' "CT" / "MR"
    Public Property Device As String              ' np. "TK1", "MR2" - na razie ręcznie
    Public Property EventType As String           ' "repeat" / "supplement"

    Public Property PatientName As String
    Public Property PatientId As String
    Public Property Accession As String

    Public Property FirstPartDateTime As DateTime
    Public Property FixRequestedByDoctor As String
    Public Property TechFirstPart As String
    Public Property Nurse As String

    Public Property ReasonId As Integer
    Public Property ReasonOtherText As String

    Public Property Description As String
End Class

Public Class RepeatEventListItem
    Public Property Id As Integer
    Public Property Modality As String
    Public Property EventType As String
    Public Property Device As String

    Public Property PatientName As String
    Public Property PatientId As String

    Public Property FirstPartDateTime As DateTime

    Public Property ReasonName As String
    Public Property ReasonOtherText As String

    Public Property Status As String
    Public Property CreatedAt As DateTime
    Public Property CreatedBy As String
End Class

Public Class RepeatEventDetails
    Public Property Id As Integer
    Public Property Modality As String
    Public Property EventType As String
    Public Property Device As String

    Public Property PatientName As String
    Public Property PatientId As String
    Public Property Accession As String

    Public Property FirstPartDateTime As DateTime
    Public Property FixRequestedByDoctor As String
    Public Property TechFirstPart As String
    Public Property Nurse As String

    Public Property ReasonName As String
    Public Property ReasonOtherText As String

    Public Property Status As String
    Public Property Description As String

    Public Property CreatedAt As DateTime
    Public Property CreatedBy As String
End Class

Public Class AuditItem
    Public Property ChangedAt As DateTime
    Public Property ChangedBy As String
    Public Property Action As String
    Public Property FieldName As String
    Public Property OldValue As String
    Public Property NewValue As String
    Public Property Machine As String
End Class

Public Class AuditLogItem
    Public Property Id As Integer
    Public Property EventId As Integer
    Public Property ChangedAt As DateTime
    Public Property ChangedBy As String
    Public Property FieldName As String
    Public Property OldValue As String
    Public Property NewValue As String
    Public Property Machine As String
End Class


