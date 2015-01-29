<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class ExperimentConsole
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.Start = New System.Windows.Forms.Button()
        Me.Finish = New System.Windows.Forms.Button()
        Me.DumpAll = New System.Windows.Forms.Button()
        Me.Generate = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'Start
        '
        Me.Start.Location = New System.Drawing.Point(98, 28)
        Me.Start.Name = "Start"
        Me.Start.Size = New System.Drawing.Size(75, 23)
        Me.Start.TabIndex = 0
        Me.Start.Text = "Start"
        Me.Start.UseVisualStyleBackColor = True
        '
        'Finish
        '
        Me.Finish.Location = New System.Drawing.Point(98, 78)
        Me.Finish.Name = "Finish"
        Me.Finish.Size = New System.Drawing.Size(75, 23)
        Me.Finish.TabIndex = 1
        Me.Finish.Text = "Finish"
        Me.Finish.UseVisualStyleBackColor = True
        '
        'DumpAll
        '
        Me.DumpAll.Location = New System.Drawing.Point(98, 137)
        Me.DumpAll.Name = "DumpAll"
        Me.DumpAll.Size = New System.Drawing.Size(75, 23)
        Me.DumpAll.TabIndex = 3
        Me.DumpAll.Text = "Dump Data"
        Me.DumpAll.UseVisualStyleBackColor = True
        '
        'Generate
        '
        Me.Generate.Location = New System.Drawing.Point(239, 78)
        Me.Generate.Name = "Generate"
        Me.Generate.Size = New System.Drawing.Size(75, 35)
        Me.Generate.TabIndex = 4
        Me.Generate.Text = "Generate Load"
        Me.Generate.UseVisualStyleBackColor = True
        '
        'ExperimentConsole
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(393, 247)
        Me.Controls.Add(Me.Generate)
        Me.Controls.Add(Me.DumpAll)
        Me.Controls.Add(Me.Finish)
        Me.Controls.Add(Me.Start)
        Me.Name = "ExperimentConsole"
        Me.Text = "ExperimentConsole"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents Start As System.Windows.Forms.Button
    Friend WithEvents Finish As System.Windows.Forms.Button
    Friend WithEvents DumpAll As System.Windows.Forms.Button
    Friend WithEvents Generate As System.Windows.Forms.Button
End Class
