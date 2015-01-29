Imports System.Text
''' <summary> </summary>
''' <remarks>This platform is implemented to facilitate research in auto-scaling and self-adaptive clouds.</remarks>
''' <authors>Pooyan Jamshidi (pooyan.jamshidi@gmail.com)</authors>
Public Class DoublesArrayEntity
    Private row As Integer
    Private col As Integer
    Public Sub New(ByVal number_of_row As Integer, ByVal number_of_col As Integer)
        row = number_of_row
        col = number_of_col
    End Sub
    Public Sub New()

    End Sub
    Public Function Array2String(ByVal array As Double()) As String
        Dim myDoubleString As String
        If array IsNot Nothing Then
            ' Build a string representing the doubles in the list, like "1.5;2.2;3.4;4.8"
            Dim sb As StringBuilder = New StringBuilder()
            For Each d In array
                sb.Append(d)
                sb.Append(",")
            Next
            'Trim the last semi-colon, so that we have "1;2;3" instead of "1;2;3;"
            myDoubleString = sb.ToString().TrimEnd(",")
        Else
            myDoubleString = Nothing
        End If
        Return myDoubleString
    End Function

    Public Function Matrix2String(ByVal matrix As Double(,)) As String
        Dim myMatrixString As String
        If matrix IsNot Nothing Then
            ' Build a string representing the doubles in the list, like "1.5;2.2;3.4;4.8"
            Dim sb As StringBuilder = New StringBuilder()
            For Each d In matrix
                sb.Append(d)
                sb.Append(",")
            Next
            'Trim the last semi-colon, so that we have "1;2;3" instead of "1;2;3;"
            myMatrixString = sb.ToString().TrimEnd(",")
        Else
            myMatrixString = Nothing
        End If
        Return myMatrixString
    End Function
End Class