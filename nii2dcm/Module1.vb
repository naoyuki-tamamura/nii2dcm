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

        Dim MatrixX As Long = 0
        Dim MatrixY As Long = 0
        Dim SliceCount As Long = 0
        Dim BigEndian As Boolean = False
        Dim RescaleSlope As Single = 1
        Dim RescaleIntercept As Single = 0
        Dim DataType As Short = 0
        Dim SizeX As Single = 0
        Dim SizeY As Single = 0
        Dim SizeZ As Single = 0
        Dim VoxelOffset As Long = 0

        'SourceFileの読み込み
        Using stream As Stream = File.OpenRead(SourceFilePath)
            ' streamから読み込むためのBinaryReaderを作成
            Using reader As New BinaryReader(stream)
                Dim HeaderBuff() As Byte = reader.ReadBytes(352)
                Dim CurrentAttribute() As Byte

                'Endian判定
                ReDim CurrentAttribute(3)
                Array.Copy(HeaderBuff, 0, CurrentAttribute, 0, 4)
                If BitConverter.ToInt32(CurrentAttribute, 0) = 348 Then
                    BigEndian = False
                Else
                    BigEndian = True
                End If

                'Matrixサイズ
                ReDim CurrentAttribute(1)
                Array.Copy(HeaderBuff, 42, CurrentAttribute, 0, 2)
                If BigEndian Then
                    Array.Reverse(CurrentAttribute)
                End If
                MatrixX = BitConverter.ToInt16(CurrentAttribute, 0)

                Array.Copy(HeaderBuff, 44, CurrentAttribute, 0, 2)
                If BigEndian Then
                    Array.Reverse(CurrentAttribute)
                End If
                MatrixY = BitConverter.ToInt16(CurrentAttribute, 0)

                'スライス枚数
                Array.Copy(HeaderBuff, 46, CurrentAttribute, 0, 2)
                If BigEndian Then
                    Array.Reverse(CurrentAttribute)
                End If
                SliceCount = BitConverter.ToInt16(CurrentAttribute, 0)

                'データタイプ
                Array.Copy(HeaderBuff, 70, CurrentAttribute, 0, 2)
                If BigEndian = True Then
                    Array.Reverse(CurrentAttribute)
                End If
                DataType = BitConverter.ToInt16(CurrentAttribute, 0)

                'ピクセルサイズ
                ReDim CurrentAttribute(3)
                Array.Copy(HeaderBuff, 80, CurrentAttribute, 0, 4)
                If BigEndian = True Then
                    Array.Reverse(CurrentAttribute)
                End If
                SizeX = BitConverter.ToSingle(CurrentAttribute, 0)

                Array.Copy(HeaderBuff, 84, CurrentAttribute, 0, 4)
                If BigEndian = True Then
                    Array.Reverse(CurrentAttribute)
                End If
                SizeY = BitConverter.ToSingle(CurrentAttribute, 0)

                Array.Copy(HeaderBuff, 88, CurrentAttribute, 0, 4)
                If BigEndian = True Then
                    Array.Reverse(CurrentAttribute)
                End If
                SizeZ = BitConverter.ToSingle(CurrentAttribute, 0)

                '画素データのオフセット
                Array.Copy(HeaderBuff, 108, CurrentAttribute, 0, 4)
                If BigEndian = True Then
                    Array.Reverse(CurrentAttribute)
                End If
                VoxelOffset = CLng(BitConverter.ToSingle(CurrentAttribute, 0))

                'スケーリングファクタ
                Array.Copy(HeaderBuff, 112, CurrentAttribute, 0, 4)
                If BigEndian = True Then
                    Array.Reverse(CurrentAttribute)
                End If
                RescaleSlope = BitConverter.ToSingle(CurrentAttribute, 0)

                Array.Copy(HeaderBuff, 116, CurrentAttribute, 0, 4)
                If BigEndian = True Then
                    Array.Reverse(CurrentAttribute)
                End If
                RescaleIntercept = BitConverter.ToSingle(CurrentAttribute, 0)

            End Using
        End Using

        Dim ImageBuff(SliceCount - 1, MatrixY - 1, MatrixX - 1) As Double
        Dim MaxValue As Double = 0

        Using stream As Stream = File.OpenRead(SourceFilePath)
            ' streamから読み込むためのBinaryReaderを作成
            Using reader As New BinaryReader(stream)
                '画素値取り込み

                Dim BytesPerPixel As Long

                Select Case DataType
                    Case 1      'Binary
                        BytesPerPixel = 1
                    Case 2      'Unsigned char
                        BytesPerPixel = 1
                    Case 4      'signed short
                        BytesPerPixel = 2
                    Case 8      'signed long
                        BytesPerPixel = 4
                    Case 16     'float
                        BytesPerPixel = 4
                    Case 64     'double
                        BytesPerPixel = 8
                    Case 512    'unsigned short
                        BytesPerPixel = 4
                    Case Else
                        Console.WriteLine("Unsupported !!")
                        Environment.Exit(2)
                End Select

                reader.ReadBytes(VoxelOffset)

                Dim AllPixelBuff() As Byte = reader.ReadBytes(MatrixX * MatrixY * SliceCount * BytesPerPixel)
                Dim CurrentPixel() As Byte

                For Z = 0 To SliceCount - 1
                    For Y = 0 To MatrixY - 1
                        For X = 0 To MatrixX - 1
                            ReDim CurrentPixel(7)

                            Array.Copy(AllPixelBuff, ((MatrixX * MatrixY * Z) + (MatrixX * Y) + X) * BytesPerPixel, CurrentPixel, 0, BytesPerPixel)
                            If BytesPerPixel > 1 And BigEndian = True Then
                                Array.Reverse(CurrentPixel)
                            End If

                            Select Case DataType
                                Case 1
                                    If CurrentPixel(0) = 0 Then
                                        ImageBuff(Z, Y, X) = 0
                                    Else
                                        ImageBuff(Z, Y, X) = 1
                                    End If
                                Case 2
                                    ImageBuff(Z, Y, X) = CDbl(BitConverter.ToInt16(CurrentPixel, 0)) * RescaleSlope + RescaleIntercept
                                Case 4
                                    ImageBuff(Z, Y, X) = CDbl(BitConverter.ToInt16(CurrentPixel, 0)) * RescaleSlope + RescaleIntercept
                                Case 8
                                    ImageBuff(Z, Y, X) = CDbl(BitConverter.ToInt32(CurrentPixel, 0)) * RescaleSlope + RescaleIntercept
                                Case 16
                                    ImageBuff(Z, Y, X) = CDbl(BitConverter.ToSingle(CurrentPixel, 0)) * RescaleSlope + RescaleIntercept
                                Case 64
                                    ImageBuff(Z, Y, X) = CDbl(BitConverter.ToDouble(CurrentPixel, 0)) * RescaleSlope + RescaleIntercept
                                Case 512
                                    ImageBuff(Z, Y, X) = CDbl(BitConverter.ToUInt16(CurrentPixel, 0)) * RescaleSlope + RescaleIntercept
                            End Select
                            If ImageBuff(Z, Y, X) > MaxValue Then
                                MaxValue = ImageBuff(Z, Y, X)
                            End If
                        Next
                    Next
                Next
            End Using
        End Using

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
        dataset.Add(DicomTag.SliceThickness, CStr(SizeZ))
        dataset.Add(DicomTag.SpacingBetweenSlices, CStr(SizeZ))
        dataset.Add(DicomTag.StudyID, String.Empty)
        dataset.Add(DicomTag.SeriesNumber, String.Empty)
        dataset.Add(DicomTag.InstanceNumber, String.Empty)
        dataset.Add(DicomTag.PatientOrientation, String.Empty)
        dataset.Add(DicomTag.SamplesPerPixel, CUShort(1))
        dataset.Add(DicomTag.PhotometricInterpretation, "MONOCHROME2")
        dataset.Add(DicomTag.PlanarConfiguration, CUShort(0))
        dataset.Add(DicomTag.Rows, CUShort(MatrixY))
        dataset.Add(DicomTag.Columns, CUShort(MatrixX))
        dataset.Add(DicomTag.PixelSpacing, CStr(SizeX) & "\" & CStr(SizeY))
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

        For Z = 0 To SliceCount - 1
            Dim Slice As New List(Of Byte)

            For Y = 0 To MatrixY - 1
                For X = 0 To MatrixX - 1
                    Dim CurrentPixel() As Byte = BitConverter.GetBytes(CShort(Truncate(ImageBuff(Z, Y, X) * DestRescale)))
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
