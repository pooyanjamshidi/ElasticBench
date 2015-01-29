Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports FibonacciLibrary
Imports Microsoft.WindowsAzure
Imports System.Configuration
Imports CsvHelper
Imports System.IO
Imports System.Windows.Forms

''' <summary></summary>
''' <remarks>This platform is implemented to facilitate research in auto-scaling and self-adaptive clouds.</remarks>
''' <authors>Pooyan Jamshidi (pooyan.jamshidi@gmail.com)</authors>
Module LoadGeneratorModule


    Sub Main()
        Dim experimentForm As New ExperimentConsole
        experimentForm.ShowDialog()

        ' Dim loadlib As New LoadLibrary

        'put the load function here
        '       loadlib.constantLoad(10, 4)
        'loadlib.testfib()



    End Sub


End Module
