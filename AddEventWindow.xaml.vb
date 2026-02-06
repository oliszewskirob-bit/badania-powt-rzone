Imports System.Globalization
Imports System.Linq
Imports System.Windows
Imports System.Windows.Controls

Public Class AddEventWindow

    Private ReadOnly _session As UserSession
    Private _reasons As List(Of ReasonItem) = New List(Of ReasonItem)()
    Private _isReady As Boolean = False

    Public Sub New(session As UserSession)
        InitializeComponent()
        _session = session

        DoctorBox.ItemsSource = StaffRepository.ListNames("doctor")
        TechBox.ItemsSource = StaffRepository.ListNames("tech")
        NurseBox.ItemsSource = StaffRepository.ListNames("nurse")

        ' minimalne ustawienia bez bazy / bez eventów
        FirstDatePicker.SelectedDate = Date.Today

        ' wszystko "cięższe" dopiero po załadowaniu okna
        AddHandler Me.Loaded, AddressOf OnLoaded
    End Sub

    Private Sub OnLoaded(sender As Object, e As RoutedEventArgs)
        ' dopinamy eventy dopiero po starcie
        AddHandler ModalityBox.SelectionChanged, AddressOf Modality_SelectionChanged
        AddHandler ReasonBox.SelectionChanged, AddressOf Reason_SelectionChanged
        AddHandler EventTypeBox.SelectionChanged, AddressOf EventType_SelectionChanged

        ' ładujemy powody (SQLite) dopiero po Loaded
        LoadReasons("CT")

        _isReady = True
        ApplyEventTypeState()
    End Sub

    Private Function SelectedModality() As String
        Dim item = TryCast(ModalityBox.SelectedItem, ComboBoxItem)
        If item Is Nothing Then Return "CT"
        Return item.Content.ToString()
    End Function

    Private Function SelectedEventType() As String
        Dim item = TryCast(EventTypeBox.SelectedItem, ComboBoxItem)
        If item Is Nothing Then Return "repeat"
        Dim tagValue = TryCast(item.Tag, String)
        If String.IsNullOrWhiteSpace(tagValue) Then
            Return "repeat"
        End If
        Return tagValue
    End Function

    Private Function IsContrastExtravasationSelected() As Boolean
        Return String.Equals(SelectedEventType(), "contrast_extravasation", StringComparison.OrdinalIgnoreCase)
    End Function

    Private Sub LoadReasons(modality As String)
        Try
            _reasons = ReasonRepository.GetReasons(modality)
            ReasonBox.ItemsSource = _reasons
            If _reasons.Count > 0 Then ReasonBox.SelectedIndex = 0
            ApplyEventTypeState()
        Catch ex As Exception
            MessageBox.Show("Błąd wczytania powodów: " & ex.Message)
        End Try
    End Sub

    Private Sub Modality_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        If Not _isReady Then Return
        LoadReasons(SelectedModality())
    End Sub

    Private Sub EventType_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        If Not _isReady Then Return
        ApplyEventTypeState()
    End Sub

    Private Sub ApplyEventTypeState()
        Dim isExtravasation = IsContrastExtravasationSelected()

        If ContrastSection IsNot Nothing Then
            ContrastSection.Visibility = If(isExtravasation, Visibility.Visible, Visibility.Collapsed)
        End If

        If isExtravasation Then
            Dim reason = _reasons.FirstOrDefault(Function(r) r.Name.ToLower().Contains("wynaczynienie kontrastu"))
            If reason IsNot Nothing Then
                ReasonBox.SelectedItem = reason
            End If

            ReasonBox.IsEnabled = False
            ReasonOtherBox.IsEnabled = False
            ReasonOtherBox.Text = ""
        Else
            ReasonBox.IsEnabled = True
            Reason_SelectionChanged(Me, Nothing)

            ContrastCannulaBox.Text = ""
            ContrastTypeBox.Text = ""
            ContrastFlowBox.Text = ""
            ContrastVolumeBox.Text = ""
            ContrastVisibleBox.SelectedItem = Nothing
            WardNotifiedBox.SelectedItem = Nothing
            PatientInstructionsBox.SelectedItem = Nothing
            ContrastAdditionalInfoBox.Text = ""
        End If
    End Sub

    Private Sub Reason_SelectionChanged(sender As Object, e As SelectionChangedEventArgs)
        If IsContrastExtravasationSelected() Then
            ReasonOtherBox.IsEnabled = False
            ReasonOtherBox.Text = ""
            Return
        End If

        Dim selected = TryCast(ReasonBox.SelectedItem, ReasonItem)
        If selected Is Nothing Then
            ReasonOtherBox.IsEnabled = False
            Return
        End If

        Dim isOther = selected.Name.ToLower().Contains("inne")
        ReasonOtherBox.IsEnabled = isOther
        If Not isOther Then ReasonOtherBox.Text = ""
    End Sub

    Private Sub Cancel_Click(sender As Object, e As RoutedEventArgs)
        Me.DialogResult = False
        Me.Close()
    End Sub

    Private Sub Save_Click(sender As Object, e As RoutedEventArgs)
        ErrorText.Text = ""

        If String.IsNullOrWhiteSpace(DeviceBox.Text) OrElse
           String.IsNullOrWhiteSpace(PatientNameBox.Text) OrElse
           String.IsNullOrWhiteSpace(PatientIdBox.Text) OrElse
           FirstDatePicker.SelectedDate Is Nothing OrElse
           String.IsNullOrWhiteSpace(DoctorBox.Text) OrElse
           String.IsNullOrWhiteSpace(TechBox.Text) OrElse
           String.IsNullOrWhiteSpace(NurseBox.Text) OrElse
           ReasonBox.SelectedItem Is Nothing Then

            ErrorText.Text = "Uzupełnij wszystkie pola oznaczone *."
            Return
        End If

        Dim datePart = FirstDatePicker.SelectedDate.Value
        Dim timeText = FirstTimeBox.Text.Trim()

        Dim timePart As DateTime
        If Not DateTime.TryParseExact(timeText, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, timePart) Then
            ErrorText.Text = "Nieprawidłowy format czasu. Użyj HH:mm (np. 08:30)."
            Return
        End If

        Dim dt1 = New DateTime(datePart.Year, datePart.Month, datePart.Day, timePart.Hour, timePart.Minute, 0)

        Dim selectedReason = CType(ReasonBox.SelectedItem, ReasonItem)
        Dim isOther = selectedReason.Name.ToLower().Contains("inne")
        If Not IsContrastExtravasationSelected() AndAlso isOther AndAlso String.IsNullOrWhiteSpace(ReasonOtherBox.Text) Then
            ErrorText.Text = "Dla 'Inne' doprecyzuj powód."
            Return
        End If

        Dim isExtravasation = IsContrastExtravasationSelected()
        Dim contrastVisible = GetYesNoValue(ContrastVisibleBox)
        Dim wardNotified = GetYesNoValue(WardNotifiedBox)
        Dim patientInstructions = GetYesNoValue(PatientInstructionsBox)

        If isExtravasation Then
            If String.IsNullOrWhiteSpace(ContrastCannulaBox.Text) OrElse
               String.IsNullOrWhiteSpace(ContrastTypeBox.Text) OrElse
               String.IsNullOrWhiteSpace(ContrastFlowBox.Text) OrElse
               String.IsNullOrWhiteSpace(ContrastVolumeBox.Text) OrElse
               Not contrastVisible.HasValue OrElse
               Not wardNotified.HasValue OrElse
               Not patientInstructions.HasValue Then
                ErrorText.Text = "Uzupełnij wszystkie pola wymagane dla wynaczynienia kontrastu."
                Return
            End If
        End If

        StaffRepository.EnsureExists("doctor", DoctorBox.Text)
        StaffRepository.EnsureExists("tech", TechBox.Text)
        StaffRepository.EnsureExists("nurse", NurseBox.Text)

        Dim ev As New RepeatEventCreate With {
            .Modality = SelectedModality(),
            .EventType = SelectedEventType(),
            .Device = DeviceBox.Text.Trim(),
            .PatientName = PatientNameBox.Text.Trim(),
            .PatientId = PatientIdBox.Text.Trim(),
            .Accession = AccessionBox.Text.Trim(),
            .FirstPartDateTime = dt1,
            .FixRequestedByDoctor = DoctorBox.Text.Trim(),
            .TechFirstPart = TechBox.Text.Trim(),
            .Nurse = NurseBox.Text.Trim(),
            .ReasonId = selectedReason.Id,
            .ReasonOtherText = ReasonOtherBox.Text.Trim(),
            .Description = DescBox.Text.Trim(),
            .IsContrastExtravasation = isExtravasation,
            .ContrastCannula = If(isExtravasation, ContrastCannulaBox.Text.Trim(), ""),
            .ContrastType = If(isExtravasation, ContrastTypeBox.Text.Trim(), ""),
            .ContrastFlow = If(isExtravasation, ContrastFlowBox.Text.Trim(), ""),
            .ContrastVolume = If(isExtravasation, ContrastVolumeBox.Text.Trim(), ""),
            .ContrastVisible = If(isExtravasation, contrastVisible, Nothing),
            .WardNotified = If(isExtravasation, wardNotified, Nothing),
            .PatientInstructions = If(isExtravasation, patientInstructions, Nothing),
            .ContrastAdditionalInfo = If(isExtravasation, ContrastAdditionalInfoBox.Text.Trim(), "")
        }

        Try
            RepeatEventsRepository.Create(ev, _session.Username)
            Me.DialogResult = True
            Me.Close()
        Catch ex As Exception
            ErrorText.Text = "Błąd zapisu: " & ex.Message
        End Try
    End Sub

    Private Function GetYesNoValue(box As ComboBox) As Boolean?
        If box Is Nothing OrElse box.SelectedItem Is Nothing Then Return Nothing
        Dim item = TryCast(box.SelectedItem, ComboBoxItem)
        If item Is Nothing OrElse item.Content Is Nothing Then Return Nothing
        Dim value = item.Content.ToString().Trim().ToLowerInvariant()
        If value = "tak" Then Return True
        If value = "nie" Then Return False
        Return Nothing
    End Function

End Class
