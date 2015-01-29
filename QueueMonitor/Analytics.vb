''' <summary></summary>
''' <remarks>This platform is implemented to facilitate research in auto-scaling and self-adaptive clouds.</remarks>
''' <authors>Pooyan Jamshidi (pooyan.jamshidi@gmail.com)</authors>
Public Class Analytics
    ' This class tracks the past counts of the number of items in the queue (workoad)
    ' In this class, we provide functions for calculating stats for decision making at runtime
    Private Property Capacity As Integer
    Private Property Items As Queue(Of Integer)

    Public Sub New(ByVal capacity As Integer)
        Me.Items = New Queue(Of Integer)
        Me.Capacity = capacity
    End Sub

    Public Sub Enqueue(ByVal item As Integer)
        Items.Enqueue(item)
        While Items.Count > Capacity
            Items.Dequeue()
        End While

    End Sub

    Public Function Dequeue() As Integer
        Return Items.Dequeue()
    End Function

    Public Function GetAverageCount() As Double
        If Items.Count = 0 Then Return 0

        Dim total As Integer
        For Each item As Integer In Items
            total += item
        Next
        Return total / Items.Count

    End Function


    Public Function GetSingleSmoothedCount(ByVal alpha As Double) As Double
        Dim n As Integer = Items.Count
        If n = 0 Then Return 0

        Dim s(n) As Double
        s(0) = 0
        s(1) = Items.ElementAt(0)
        For i = 2 To n
            s(i) = alpha * Items.ElementAt(i - 1) + (1 - alpha) * s(i - 1)
        Next
        Return s(n)
    End Function

    Public Function GetDoubleSmoothedCount(ByVal alpha As Double, ByVal gamma As Double) As Double
        Dim n As Integer = Items.Count
        If n = 0 Then Return 0
        If n = 1 Then Return Items.ElementAt(0)

        Dim s(n - 1) As Double
        Dim b(n - 1) As Double
        s(0) = Items.ElementAt(0)
        b(0) = Items.ElementAt(1) - Items.ElementAt(0)
        For i = 1 To n - 1
            s(i) = alpha * Items.ElementAt(i) + (1 - alpha) * (s(i - 1) + b(i - 1))
            b(i) = gamma * (s(i) - s(i - 1)) + (1 - gamma) * b(i - 1)
        Next
        Return s(n - 1) + b(n - 1)
    End Function

    Public Function GetPercentile(percentile As Integer) As Double
        If Items.Count = 0 Then Return 0
        Return Items.OrderBy(Function(n) n).Skip(CInt(Math.Floor(Items.Count * percentile / 100))).First()
    End Function

    Public Function GetMax() As Integer
        If Items.Count = 0 Then Return 0
        Return Items.Max
    End Function

    Public Function GetMin() As Integer
        If Items.Count = 0 Then Return 0
        Return Items.Min
    End Function

End Class
