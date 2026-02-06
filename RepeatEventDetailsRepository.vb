Imports Microsoft.Data.Sqlite
Imports System.Globalization

Public Module RepeatEventDetailsRepository

    Public Function GetById(id As Integer) As RepeatEventDetails
        Using con = Db.Open()
            Using cmd = con.CreateCommand()
                cmd.CommandText =
"SELECT
    e.Id,
    e.Modality,
    e.EventType,
    e.Device,
    e.PatientName,
    e.PatientId,
    COALESCE(e.Accession, '') AS Accession,
    e.FirstPartDateTime,
    COALESCE(e.FixRequestedByDoctor, '') AS FixRequestedByDoctor,
    COALESCE(e.TechFirstPart, '') AS TechFirstPart,
    COALESCE(e.Nurse, '') AS Nurse,
    COALESCE(r.Name, '') AS ReasonName,
    COALESCE(e.ReasonOtherText, '') AS ReasonOtherText,
    COALESCE(e.Status, 'new') AS Status,
    COALESCE(e.Description, '') AS Description,
    COALESCE(e.IsContrastExtravasation,0) AS IsContrastExtravasation,
    COALESCE(e.ContrastCannula,'') AS ContrastCannula,
    COALESCE(e.ContrastType,'') AS ContrastType,
    COALESCE(e.ContrastFlow,'') AS ContrastFlow,
    COALESCE(e.ContrastVolume,'') AS ContrastVolume,
    COALESCE(e.ContrastVisible,0) AS ContrastVisible,
    COALESCE(e.WardNotified,0) AS WardNotified,
    COALESCE(e.PatientInstructions,0) AS PatientInstructions,
    COALESCE(e.ContrastAdditionalInfo,'') AS ContrastAdditionalInfo,
    e.CreatedAt,
    COALESCE(e.CreatedBy, '') AS CreatedBy
FROM RepeatEvents e
LEFT JOIN Reasons r ON r.Id = e.ReasonId
WHERE e.Id = $id
LIMIT 1;"
                cmd.Parameters.AddWithValue("$id", id)

                Using r = cmd.ExecuteReader()
                    If Not r.Read() Then Return Nothing

                    Dim dt1 As DateTime = ParseDateTimeOrMin(r.GetString(7))
                    Dim created As DateTime = ParseDateTimeOrMin(r.GetString(24))
                    Dim eventType = r.GetString(2)
                    Dim status = r.GetString(13)

                    Return New RepeatEventDetails With {
                        .Id = r.GetInt32(0),
                        .Modality = r.GetString(1),
                        .EventType = eventType,
                        .EventTypeLabel = EventTypeLabel(eventType),
                        .Device = r.GetString(3),
                        .PatientName = r.GetString(4),
                        .PatientId = r.GetString(5),
                        .Accession = r.GetString(6),
                        .FirstPartDateTime = dt1,
                        .FixRequestedByDoctor = r.GetString(8),
                        .TechFirstPart = r.GetString(9),
                        .Nurse = r.GetString(10),
                        .ReasonName = r.GetString(11),
                        .ReasonOtherText = r.GetString(12),
                        .Status = status,
                        .StatusLabel = StatusLabel(status),
                        .Description = r.GetString(14),
                        .IsContrastExtravasation = (r.GetInt32(15) = 1),
                        .ContrastCannula = r.GetString(16),
                        .ContrastType = r.GetString(17),
                        .ContrastFlow = r.GetString(18),
                        .ContrastVolume = r.GetString(19),
                        .ContrastVisible = (r.GetInt32(20) = 1),
                        .WardNotified = (r.GetInt32(21) = 1),
                        .PatientInstructions = (r.GetInt32(22) = 1),
                        .ContrastAdditionalInfo = r.GetString(23),
                        .CreatedAt = created,
                        .CreatedBy = r.GetString(25)
                    }
                End Using
            End Using
        End Using
    End Function

End Module
