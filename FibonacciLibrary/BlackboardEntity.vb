''' <summary></summary>
''' <remarks>This platform is implemented to facilitate research in auto-scaling and self-adaptive clouds.</remarks>
''' <authors>Pooyan Jamshidi (pooyan.jamshidi@gmail.com)</authors>
Public Class BlackboardEntity
    Inherits Microsoft.WindowsAzure.Storage.Table.TableEntity

    Public Sub New(ByVal partitionKey As String, ByVal blackboardItem As String)
        Me.PartitionKey = partitionKey
        Me.RowKey = blackboardItem
    End Sub

    Public Sub New()
        'your entity type must expose a parameter-less constructor.
    End Sub

    Private _itemValue As Integer
    Public Property ItemValue() As Integer
        Get
            Return _itemValue
        End Get
        Set(value As Integer)
            _itemValue = value
        End Set
    End Property
    Public Enum RoleMode
        Idle = 0
        ExperimentRunning = 1
    End Enum

End Class
