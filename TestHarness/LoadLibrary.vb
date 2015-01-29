Imports System.Threading

''' <summary></summary>
''' <remarks>This platform is implemented to facilitate research in auto-scaling and self-adaptive clouds.</remarks>
''' <authors>Pooyan Jamshidi (pooyan.jamshidi@gmail.com)</authors>
Public Class LoadLibrary
    ' in this class we put different load patterns in order to nject different loads to the queue


    Private expected() As Integer = {0, 1, 1, 2, 3, 5, 8, _
     13, 21, 34, 55, 89, 144, _
     233, 377, 610, 987, 1597, _
     2584, 4181, 6765}

    Public Sub testfib()

        Dim svc As New Fibonacci.FibonacciClient
        Try
            ' checking to see that the service responds
            Dim pingResult As Boolean = svc.Ping

            Dim fibvalue As Int64 = 0
            Dim watch As Stopwatch = Stopwatch.StartNew

            If pingResult Then
                Console.WriteLine("Ping succeeded")

                For i = 0 To 20
                    watch.Start()
                    fibvalue = svc.Calculate(i)
                    watch.Stop()
                    Console.WriteLine("F({0}){1}{2}{3}{4}{5}{6}", i, vbTab, fibvalue, vbTab, expected(i), vbTab, watch.ElapsedMilliseconds)
                Next
            End If
        Catch ex As Exception
            Console.WriteLine("Ping failed. {0}", ex.Message)
        End Try

        Console.ReadLine()

    End Sub


    Public Sub constantLoad(ByVal number_of_messages As Integer, ByVal load_level As Integer)
        Dim svc As New Fibonacci.FibonacciClient
        Dim pingResult As Boolean

        Try
            pingResult = svc.Ping
            If pingResult Then
                Console.WriteLine("ping succeeded")

                For i = 0 To number_of_messages
                    svc.BeginCalculate(load_level)
                Next

            End If
        Catch ex As Exception
            Console.WriteLine("Ping failed. {0}", ex.Message)
        End Try
    End Sub

    Public Sub replayLoad(ByVal load_levels As Integer(), ByVal time_interval As Integer)
        Dim svc As New Fibonacci.FibonacciClient
        Dim pingResult As Boolean

        Try
            pingResult = svc.Ping
            If pingResult Then
                Console.WriteLine("ping succeeded")

                For Each load_level In load_levels

                    svc.BeginCalculate(load_level)
                    Thread.Sleep(time_interval)

                Next

            End If
        Catch ex As Exception
            Console.WriteLine("Ping failed. {0}", ex.Message)
        End Try
    End Sub

    Public Sub uniformLoad(ByVal test_time As Integer)
        Dim svc As New Fibonacci.FibonacciClient
        Dim pingResult As Boolean

        Try
            pingResult = svc.Ping
            If pingResult Then
                Console.WriteLine("ping succeeded")

                Dim watch As Stopwatch = Stopwatch.StartNew
                watch.Start()

                Dim rndload As New System.Random
                Dim rndtime As New System.Random

                While watch.ElapsedMilliseconds <= test_time
                    Try
                        svc.BeginCalculate(Math.Round(rndload.Next(1, 10)))
                        Thread.Sleep(rndtime.Next(50))
                    Catch ex As Exception

                    End Try

                End While
                watch.Stop()

            End If
        Catch ex As Exception
            Console.WriteLine("Ping failed. {0}", ex.Message)
        End Try
    End Sub

End Class
