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
    e.CreatedAt,
    COALESCE(e.CreatedBy, '') AS CreatedBy
FROM RepeatEvents e
LEFT JOIN Reasons r ON r.Id = e.ReasonId
WHERE e.Id = $id
LIMIT 1;"
                cmd.Parameters.AddWithValue("$id", id)

                Using r = cmd.ExecuteReader()
                    If Not r.Read() Then Return Nothing

                    Dim dt1 As DateTime = DateTime.Parse(r.GetString(7), Nothing, DateTimeStyles.RoundtripKind)
                    Dim created As DateTime = DateTime.Parse(r.GetString(15), Nothing, DateTimeStyles.RoundtripKind)

                    Return New RepeatEventDetails With {
                        .Id = r.GetInt32(0),
                        .Modality = r.GetString(1),
                        .EventType = r.GetString(2),
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
                        .Status = r.GetString(13),
                        .Description = r.GetString(14),
                        .CreatedAt = created,
                        .CreatedBy = r.GetString(16)
                    }
                End Using
            End Using
        End Using
    End Function

End Module
