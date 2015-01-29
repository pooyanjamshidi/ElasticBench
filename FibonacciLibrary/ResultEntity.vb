''' <summary> </summary>
''' <remarks>This platform is implemented to facilitate research in auto-scaling and self-adaptive clouds.</remarks>
''' <authors>Pooyan Jamshidi (pooyan.jamshidi@gmail.com)</authors>
Public Class ResultEntity
    Inherits Microsoft.WindowsAzure.Storage.Table.TableEntity

    Public Sub New()
        Me.PartitionKey = String.Empty
        Me.RowKey = DateTime.Now.Ticks.ToString("d19")
        Me.TimeInserted = DateTime.Now
    End Sub

    Public Property TimeInserted As DateTime
    Public Property RequestCount As Integer
    Public Property AverageRequestCount As Double
    Public Property AverageRequestPerSec As Double
    Public Property AverageRequestCountN As Double
    Public Property ProcessedCount As Integer
    Public Property ProcessedCountCache As Integer
    Public Property ProcessedCountTable As Integer
    Public Property QueueSize As Integer
    Public Property AveItemCount As Double
    Public Property AverageThroughput As Double
    Public Property AverageThroughputPerMilliseconds As Double
    Public Property AverageReponseTime As Double
    Public Property AverageResponseTimeN As Double
    Public Property ControlOutput As Double
    Public Property SingleSmoothed As Double
    Public Property DoubleSmoothed As Double
    Public Property Percentile As Double
    Public Property ScalingDecision As Integer
    Public Property ActionIssued As Boolean
    Public Property SuccessfullyIssued As Boolean
    Public Property Enacted As Integer
    Public Property MonitoringDelay As Long
    Public Property PlanningDelay As Long
    Public Property ActuationDelay As Long
    Public Property ElapsedTimeInFeedbackLoop As Long
    Public Property AveElapsedTimeInFeedbackLoop As Double
    'learning data
    Public Property CurrentState As String
    Public Property LEARNmatrix As String
    Public Property Qtable As String
    Public Property Epsilon As Double
    Public Property Epoch As Integer

End Class
