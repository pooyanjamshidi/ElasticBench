''' <summary></summary>
''' <remarks>This platform is implemented to facilitate research in auto-scaling and self-adaptive clouds.</remarks>
''' <authors>Pooyan Jamshidi (pooyan.jamshidi@gmail.com)</authors>
Public Class Generator

    ' By definition, F(0) = 0 and F(1) = 1. After that, F(n) = F(n-2) + F(n-1). 


    Public Shared Function Calculate(ByVal n As Integer) As Int64
        If n = 0 Then Return 0
        If n = 1 Then Return 1

        Dim firstNumber As Int64 = 0
        Dim lastNumber As Int64 = 1

        Dim temp As Int64

        For i As Integer = 2 To n
            temp = lastNumber
            lastNumber += firstNumber
            firstNumber = temp
            ' The presence of a Thread.Sleep call ensures that this code takes a fairly long time to complete, simulating real processes with different response time.
            ' This simulate the delay in writing the value to a database.
            Threading.Thread.Sleep(100)
        Next

        Return lastNumber
    End Function




End Class
