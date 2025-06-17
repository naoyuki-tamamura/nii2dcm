Imports Dicom
Imports Dicom.Imaging
Imports Dicom.IO.Buffer
Imports System.IO
Imports System.Math

Module Module1

    Sub Main(args() As String)

        Dim SourceFilePath As String = Nothing
        If args.Length < 1 Then
            Console.WriteLine("usage hogehoge.exe Source.nii")
            Environment.Exit(1)
        Else
            SourceFilePath = args(0)
        End If

        Dim FileName As String = Path.GetFileNameWithoutExtension(SourceFilePath)
        Dim dcmFilePath As String = Path.ChangeExtension(SourceFilePath, "dcm")

        If System.IO.File.Exists(SourceFilePath) = False Then
            Console.WriteLine(SourceFilePath & " が見つかりません。")
            Environment.Exit(2)
        ElseIf System.IO.File.Exists(dcmFilePath) = True Then
            Console.WriteLine(dcmFilePath & "が存在します。")
            Environment.Exit(2)
        End If

        'NIfTIファイルの読み込み
        Dim Source As New clsNIfTIFile
        Source.Read(SourceFilePath)

        '右手座標から左手座標に変換
        Source.FlipDimension(0)
        Source.FlipDimension(1)

        '最大値探索
        Dim MaxValue As Double = Source.GetPixelMax

        'スケーリングファクタ設定
        Dim DestRescale As Single
        If 32768 / MaxValue >= 10000 Then
            DestRescale = 10000
        ElseIf 32768 / MaxValue >= 1000 Then
            DestRescale = 1000
        ElseIf 32768 / MaxValue >= 100 Then
            DestRescale = 100
        ElseIf 32768 / MaxValue >= 10 Then
            DestRescale = 10
        ElseIf 32768 / MaxValue >= 1 Then
            DestRescale = 1
        ElseIf 32768 / MaxValue >= 0.1 Then
            DestRescale = 0.1
        ElseIf 32768 / MaxValue >= 0.01 Then
            DestRescale = 0.01
        ElseIf 32768 / MaxValue >= 0.001 Then
            DestRescale = 0.001
        ElseIf 32768 / MaxValue >= 0.0001 Then
            DestRescale = 0.0001
        End If

        'DICOMファイル作成
        Dim dataset As New DicomDataset()

        dataset.Add(DicomTag.SOPClassUID, DicomUID.NuclearMedicineImageStorage)
        dataset.Add(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID().UID)
        dataset.Add(DicomTag.StudyInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID().UID)
        dataset.Add(DicomTag.SeriesInstanceUID, DicomUIDGenerator.GenerateDerivedFromUUID().UID)

        dataset.Add(DicomTag.Modality, "NM")
        dataset.Add(DicomTag.SpecificCharacterSet, String.Empty)
        dataset.Add(DicomTag.ImageType, "DERIVED\PRIMARY\RECON TOMO\EMISSION")
        dataset.Add(DicomTag.StudyDate, DateTime.Now)
        dataset.Add(DicomTag.SeriesDate, DateTime.Now)
        dataset.Add(DicomTag.ContentDate, DateTime.Now)
        dataset.Add(DicomTag.StudyTime, DateTime.Now)
        dataset.Add(DicomTag.SeriesTime, DateTime.Now)
        dataset.Add(DicomTag.ContentTime, DateTime.Now)
        dataset.Add(DicomTag.AccessionNumber, String.Empty)
        dataset.Add(DicomTag.ReferringPhysicianName, String.Empty)
        dataset.Add(DicomTag.StudyDescription, "Study")
        dataset.Add(DicomTag.SeriesDescription, "Series")
        dataset.Add(DicomTag.PatientName, FileName)
        dataset.Add(DicomTag.PatientID, FileName)
        dataset.Add(DicomTag.PatientBirthDate, String.Empty)
        dataset.Add(DicomTag.PatientSex, String.Empty)
        dataset.Add(DicomTag.SliceThickness, CStr(Source.SizeZ))
        dataset.Add(DicomTag.SpacingBetweenSlices, CStr(Source.SizeZ))
        dataset.Add(DicomTag.StudyID, String.Empty)
        dataset.Add(DicomTag.SeriesNumber, String.Empty)
        dataset.Add(DicomTag.InstanceNumber, String.Empty)
        dataset.Add(DicomTag.PatientOrientation, String.Empty)
        dataset.Add(DicomTag.SamplesPerPixel, CUShort(1))
        dataset.Add(DicomTag.PhotometricInterpretation, "MONOCHROME2")
        dataset.Add(DicomTag.PlanarConfiguration, CUShort(0))
        dataset.Add(DicomTag.Rows, CUShort(Source.MatrixY))
        dataset.Add(DicomTag.Columns, CUShort(Source.MatrixX))
        dataset.Add(DicomTag.PixelSpacing, CStr(Source.SizeX) & "\" & CStr(Source.SizeY))
        dataset.Add(DicomTag.BitsAllocated, CUShort(16))
        dataset.Add(DicomTag.BitsStored, CUShort(16))
        dataset.Add(DicomTag.HighBit, CUShort(15))
        dataset.Add(DicomTag.PixelRepresentation, CUShort(1))
        dataset.Add(DicomTag.WindowCenter, "0.0")
        dataset.Add(DicomTag.WindowWidth, "0.0")
        dataset.Add(DicomTag.RescaleIntercept, "0.0")
        dataset.Add(DicomTag.RescaleSlope, CStr(1 / DestRescale))


        Dim RIinfo As New DicomDataset()
        RIinfo.Add(DicomTag.Radiopharmaceutical, "IMP")
        dataset.Add(New DicomSequence(DicomTag.RadiopharmaceuticalInformationSequence, RIinfo))

        dataset.Add(New DicomOtherWord(DicomTag.PixelData, New CompositeByteBuffer()))

        Dim PixelData As DicomPixelData = DicomPixelData.Create(dataset, True)

        For z As Integer = 0 To Source.MatrixZ - 1
            Dim Slice As New List(Of Byte)

            For y As Integer = 0 To Source.MatrixY - 1
                For x As Integer = 0 To Source.MatrixX - 1
                    Dim CurrentPixel() As Byte = BitConverter.GetBytes(Convert.ToInt16(Truncate(Source.Pixel(x, y, z) * DestRescale)))
                    Slice.Add(CurrentPixel(0))
                    Slice.Add(CurrentPixel(1))
                Next
            Next
            Dim SliceBuff As New MemoryByteBuffer(Slice.ToArray())
            PixelData.AddFrame(SliceBuff)
        Next

        dataset.Validate()

        Dim dicomFile As New DicomFile(dataset)
        dicomFile.FileMetaInfo.SourceApplicationEntityTitle = "My"

        dicomFile.Save(dcmFilePath)

    End Sub

End Module
