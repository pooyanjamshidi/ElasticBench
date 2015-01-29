Imports CsvHelper
Imports System.IO
Imports System.Text

''' <summary></summary>
''' <remarks>This platform is implemented to facilitate research in auto-scaling and self-adaptive clouds.</remarks>
''' <authors>Pooyan Jamshidi (pooyan.jamshidi@gmail.com)</authors>
Public Class DataRecord
    ' data structure for experimental data
    Public RequestCount As Integer
    Public ProcessedCount As Integer
    Public QueueSize As Integer
    Public AveQueueSize As Integer
    Public ScalingDecision As Integer
    Public Enacted As Boolean

End Class

Public Class DataExporter

    Private textWriter As StreamWriter
    Private csv As CsvWriter
    Public Sub New()
        textWriter = New StreamWriter("data.csv")
        textWriter.AutoFlush = True
        csv = New CsvWriter(textWriter)
    End Sub

    Public Sub WriteDataPoint(record As datarecord)
        csv.WriteField(record)
    End Sub

    Public Sub DumpData()
        textWriter.Close()
    End Sub

End Class
