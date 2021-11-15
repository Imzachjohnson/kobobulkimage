
Imports System.ComponentModel
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Net
Imports System.Threading
Imports TextFileReader
Imports Amazon.S3
Imports Amazon.S3.Model
Imports Amazon.Runtime
Imports Amazon
Imports Amazon.S3.Util
Imports Amazon.S3.Transfer
Imports System.Reflection


Public Class Form1

    Public Shared bucketName As String = "webapp.company.com"
    Public Const downloadpathtemp = ""

    Private Shared ReadOnly bucketRegion As RegionEndpoint = RegionEndpoint.USEast1
        Private Shared s3Client As IAmazonS3

    Public workerURLs As New Queue(Of DataGridViewRow)()
        Public workerURLsLock As Object = "LOCK"
        Public bgws As New List(Of BackgroundWorker)
        Public blankcount As Integer = 1

        Public failed As New List(Of DataGridViewRow)
        Public completed As Integer = 0

        Public running As Boolean = False

        Public KOBOURL = $"https://kc.humanitarianresponse.info/attachment/original?media_file=username/attachments/"

        Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        s3secret.Text = My.Settings.s3secret
            s3access.Text = My.Settings.s3access
            TextBox3.Text = My.Settings.kobousername
            bucket.Text = My.Settings.s3bucket


            CheckForIllegalCrossThreadCalls = False
        DataGridView2.Columns.Add("Status", "Status")

            DataGridView2.RowHeadersVisible = False


            DataGridView1.RowHeadersVisible = False
        EnableDoubleBuffered(DataGridView1)


    End Sub


    Public Sub EnableDoubleBuffered(ByVal dgv As DataGridView)

        Dim dgvType As Type = dgv.[GetType]()

        Dim pi As PropertyInfo = dgvType.GetProperty("DoubleBuffered",
                                                 BindingFlags.Instance Or BindingFlags.NonPublic)

        pi.SetValue(dgv, True, Nothing)

    End Sub

    Private Sub MoveColumns()

            For Each column In DataGridView1.SelectedColumns
                DataGridView1.Columns.Add(CType(column.Clone(), DataGridViewColumn))


            Next

        End Sub



        Private Sub Button1_Click_1(sender As Object, e As EventArgs) Handles Button1.Click
            Dim fd As OpenFileDialog = New OpenFileDialog()
            Dim strFileName As String

            fd.Title = "Open File Dialog"
            fd.Filter = "CSV Files (*.csv)|*.csv"
            fd.FilterIndex = 2
            fd.RestoreDirectory = True

        If fd.ShowDialog() = DialogResult.OK Then
            TextBox1.Text = fd.FileName


            Call Task.Run(Sub()

                              'Define DataTable
                              Dim TextFileReader As New Microsoft.VisualBasic.FileIO.TextFieldParser(TextBox1.Text)

                              TextFileReader.TextFieldType = FileIO.FieldType.Delimited
                              TextFileReader.SetDelimiters(";")

                              Dim TextFileTable As DataTable = Nothing

                              Dim Column As DataColumn
                              Dim Row As DataRow
                              Dim UpperBound As Int32
                              Dim ColumnCount As Int32
                              Dim CurrentRow As String()

                              While Not TextFileReader.EndOfData
                                  Try

                                      CurrentRow = TextFileReader.ReadFields()
                                      Label1.Invoke(New Action(Sub() ToolStripStatusLabel1.Text = "Reading CSV..."))
                                      If Not CurrentRow Is Nothing Then
                                          ''# Check if DataTable has been created
                                          If TextFileTable Is Nothing Then
                                              TextFileTable = New DataTable("TextFileTable")
                                              ''# Get number of columns
                                              UpperBound = CurrentRow.GetUpperBound(0)
                                              ''# Create new DataTable
                                              For ColumnCount = 0 To UpperBound
                                                  Column = New DataColumn()
                                                  Column.DataType = System.Type.GetType("System.String")
                                                  Column.ColumnName = "Column" & ColumnCount
                                                  Column.Caption = "Column" & ColumnCount
                                                  Column.ReadOnly = True
                                                  Column.Unique = False
                                                  TextFileTable.Columns.Add(Column)
                                              Next
                                          End If
                                          Row = TextFileTable.NewRow
                                          For ColumnCount = 0 To UpperBound
                                              Row("Column" & ColumnCount) = CurrentRow(ColumnCount).ToString
                                          Next

                                          TextFileTable.Rows.Add(Row)


                                      End If
                                  Catch ex As _
                                    Microsoft.VisualBasic.FileIO.MalformedLineException

                                  End Try
                              End While

                              'Set DataSource
                              DataGridView1.Invoke(New Action(Sub() DataGridView1.DataSource = TextFileTable))




                              Label1.Invoke(New Action(Sub() ToolStripStatusLabel1.Text = "CSV file loaded. Total rows: " + DataGridView1.Rows.Count.ToString))

                              For Each col As DataGridViewColumn In DataGridView1.Columns
                                  DataGridView1.Invoke(New Action(Sub() col.SortMode = DataGridViewColumnSortMode.NotSortable))
                              Next

                              DataGridView1.Invoke(New Action(Sub() DataGridView1.SelectionMode = DataGridViewSelectionMode.ColumnHeaderSelect))

                          End Sub)

        End If

    End Sub



        Sub download()

            Dim download_url = "https://kc.humanitarianresponse.info/attachment/original?media_file=" + TextBox3.Text + "/attachments/"

        End Sub


        Private Sub Button2_Click_1(sender As Object, e As EventArgs) Handles Button2.Click

        If DataGridView1.Rows.Count = 0 Then
            MessageBox.Show(" Please load a CSV first.")

            Return
        End If

        Call Task.Run(Sub()
                              Label1.Invoke(New Action(Sub() ToolStripStatusLabel1.Text = "Processing column data..."))
                              If (DataGridView2.ColumnCount = 1) Then
                                  DataGridView2.Columns.Add("Images", "Images")

                              End If

                              Try
                                  For Each cell As DataGridViewCell In DataGridView1.SelectedCells
                                      If Not String.IsNullOrWhiteSpace(cell.Value) And cell.Value.Contains(".jpg") Then
                                          DataGridView1.Invoke(New Action(Sub() DataGridView2.Rows.Add("Not Processed", cell.Value)))
                                      End If

                                      Label1.Invoke(New Action(Sub() ToolStripStatusLabel1.Text = "Total Images: " + DataGridView2.Rows.Count.ToString))

                                  Next
                              Catch

                              End Try
                          End Sub)


        End Sub


        Private Shared Async Function UploadFileAsync(filepath As String, keyname As String) As Task

            Dim req As WebRequest = HttpWebRequest.Create(filepath)
            Using stream As Stream = req.GetResponse().GetResponseStream()

                Dim fileTransferUtility = New TransferUtility(s3Client)

                Await fileTransferUtility.UploadAsync(stream, bucketName, keyname)
                My.Computer.FileSystem.DeleteFile(filepath)
            End Using


        End Function




        Public Function TestRotate(sImageFilePath As String) As Boolean
            Dim rft As RotateFlipType = RotateFlipType.RotateNoneFlipNone
            Dim img As Bitmap = Image.FromFile(sImageFilePath)
            Dim properties As PropertyItem() = img.PropertyItems
            Dim bReturn As Boolean = False
            For Each p As PropertyItem In properties
                If p.Id = 274 Then
                    Dim orientation As Short = BitConverter.ToInt16(p.Value, 0)
                    Select Case orientation
                        Case 1
                            rft = RotateFlipType.RotateNoneFlipNone
                        Case 3
                            rft = RotateFlipType.Rotate180FlipNone
                        Case 6
                            rft = RotateFlipType.Rotate270FlipNone
                        Case 8
                            rft = RotateFlipType.Rotate270FlipNone
                    End Select
                End If
            Next
            If rft <> RotateFlipType.RotateNoneFlipNone Then
                img.RotateFlip(rft)
                System.IO.File.Delete(sImageFilePath)
                img.Save(sImageFilePath, System.Drawing.Imaging.ImageFormat.Jpeg)
                bReturn = True
            End If
            Return bReturn

        End Function







        'thread pool

        Private Sub bgw_DoWork(ByVal sender As Object, ByVal e As DoWorkEventArgs)


            Dim bgw As BackgroundWorker = DirectCast(sender, BackgroundWorker)

            Dim workToDo As Boolean = True

            While workToDo
                Dim nextUrl As DataGridViewRow
                SyncLock workerURLsLock
                    Try
                        nextUrl = workerURLs.Dequeue()
                    Catch
                    End Try

                End SyncLock

                If workerURLs.Count > 0 Then
                    If bgw.CancellationPending Then
                        bgw.CancelAsync()
                        e.Cancel = True
                        Return
                    End If


                    Dim Source = ""

                    If large.Checked Then
                        Source = "https://kc.humanitarianresponse.info/attachment/original?media_file=" + TextBox3.Text + "/attachments/" + nextUrl.Cells(1).Value
                    End If

                    If medium.Checked Then
                        Source = "https://kc.humanitarianresponse.info/attachment/medium?media_file=" + TextBox3.Text + "/attachments/" + nextUrl.Cells(1).Value
                    End If

                    If small.Checked Then
                        Source = "https://kc.humanitarianresponse.info/attachment/small?media_file=" + TextBox3.Text + "/attachments/" + nextUrl.Cells(1).Value
                    End If

                    Try
                        'do work here

                        nextUrl.Cells(0).Value = "Processing..."


                        'local download
                        Using webClient As WebClient = New WebClient()
                            Dim data As Byte() = webClient.DownloadData(Source)

                            Using mem As MemoryStream = New MemoryStream(data)

                                Using yourImage = Image.FromStream(mem)
                                    ' If you want it as Jpeg
                                    yourImage.Save("C:\Users\Zach\Desktop\images\" + nextUrl.Cells(1).Value, ImageFormat.Jpeg)




                                    If correcrotationcheck.Checked Then
                                        nextUrl.Cells(0).Value = "Correcting Rotation"
                                        TestRotate("C:\Users\Zach\Desktop\images\" + nextUrl.Cells(1).Value)
                                    End If


                                    If amazons3.Checked Then
                                        nextUrl.Cells(0).Value = "Uploading to S3..."
                                        Dim credentials = New BasicAWSCredentials(s3access.Text, s3secret.Text)
                                        bucketName = bucket.Text
                                        s3Client = New AmazonS3Client(credentials, bucketRegion)
                                        UploadFileAsync("C:\Users\Zach\Desktop\images\" + nextUrl.Cells(1).Value, nextUrl.Cells(1).Value).Wait()

                                    End If

                                End Using
                            End Using
                        End Using

                        bgw.ReportProgress(1, "Completed: * " + nextUrl.Index.ToString)
                    Catch s As Exception
                        failed.Add(nextUrl)
                        nextUrl.Cells(0).Value = "Failed"
                        nextUrl.Cells(0).Style.BackColor = Color.Red
                    End Try
                Else
                    workToDo = False
                End If
            End While
        End Sub


        Private Sub bgw_ProgressChanged(ByVal sender As Object, ByVal e As System.ComponentModel.ProgressChangedEventArgs)
            Dim bgw As BackgroundWorker = DirectCast(sender, BackgroundWorker)
            Dim data() As String = Split(DirectCast(e.UserState, String), "*", , CompareMethod.Text)


            If data(0).Contains("Completed") Then
                completed += 1
                success.Text = "Success: " + completed.ToString
                DataGridView2.Rows(Convert.ToInt32(data(1))).Cells(0).Value = "Success"
            End If
            failedlabel.Text = "Failed: " + failed.Count.ToString
            remaining.Text = "Remaining: " + workerURLs.Count.ToString
        End Sub

        Private Sub bgw_RunWorkerCompleted(ByVal sender As Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs)
            Dim bgw As BackgroundWorker = DirectCast(sender, BackgroundWorker)
            bgws.Remove(bgw)
            bgw.Dispose()

            If bgws.Count = 0 Then
                downloadbutton.Text = "Download Images"
                running = False
                workerURLs.Clear()
                completed = 0
                MessageBox.Show("Done!")
            End If
        End Sub

        Private Sub startworkers()

            For Each row As DataGridViewRow In DataGridView2.Rows
                workerURLs.Enqueue(row)
            Next

            For i As Integer = 0 To NumericUpDown1.Value

                Dim bgw As New BackgroundWorker()

                bgw.WorkerReportsProgress = True

                bgw.WorkerSupportsCancellation = True

                AddHandler bgw.DoWork, New DoWorkEventHandler(AddressOf bgw_DoWork)

                AddHandler bgw.ProgressChanged, New ProgressChangedEventHandler(AddressOf bgw_ProgressChanged)

                AddHandler bgw.RunWorkerCompleted, New RunWorkerCompletedEventHandler(AddressOf bgw_RunWorkerCompleted)
                bgws.Add(bgw)
                'Start The Worker 
                bgw.RunWorkerAsync()

            Next

        End Sub



        Private Sub Button3_Click(sender As Object, e As EventArgs) Handles downloadbutton.Click
            If amazons3.Checked Then

                If String.IsNullOrEmpty(s3secret.Text) Then
                    MessageBox.Show("Please enter your S3 secret key.")
                    Return
                End If

                If String.IsNullOrEmpty(s3access.Text) Then
                    MessageBox.Show("Please enter your S3 access key.")
                    Return
                End If

            End If

            If NumericUpDown1.Value = 0 Then
                MessageBox.Show("Conncurrent downloads cannot be 0.")
                Return
            End If

            If DataGridView2.Rows.Count = 0 Then
                MessageBox.Show("Please add some data columns first.")
                TabControl1.SelectedTab = TabPage1
                Return
            End If

            If String.IsNullOrEmpty(TextBox3.Text) Then
                MessageBox.Show("Please enter you Kobo username in the settings pane.")
                Return
            End If

            If running = True Then
                For Each bgw As BackgroundWorker In bgws
                    bgw.CancelAsync()
                Next

            End If

            If running = False Then
                startworkers()
                running = True
                downloadbutton.Text = "Cancel"
            End If

        End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs)
        Dim result As DialogResult = MessageBox.Show("Are you sure you want to clear all data from the table??",
                              "Clear Data",
                              MessageBoxButtons.YesNo)

        If (result = DialogResult.Yes) Then
            DataGridView1.DataSource = Nothing


            ToolStripStatusLabel1.Text = "Idle"


        Else
            Return
        End If
    End Sub

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        My.Settings.s3secret = s3secret.Text
        My.Settings.s3access = s3access.Text
        My.Settings.kobousername = TextBox3.Text
        My.Settings.s3bucket = bucket.Text
        My.Settings.Save()
    End Sub
End Class
