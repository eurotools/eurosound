<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Frm_TimerForm
    Inherits System.Windows.Forms.Form

    'Form reemplaza a Dispose para limpiar la lista de componentes.
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

    'Requerido por el Diseñador de Windows Forms
    Private components As System.ComponentModel.IContainer

    'NOTA: el Diseñador de Windows Forms necesita el siguiente procedimiento
    'Se puede modificar usando el Diseñador de Windows Forms.  
    'No lo modifique con el editor de código.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Frm_TimerForm))
        Me.Groupbox_TaskTime = New System.Windows.Forms.GroupBox()
        Me.ProgressBar1 = New System.Windows.Forms.ProgressBar()
        Me.Groupbox_TaskTime.SuspendLayout()
        Me.SuspendLayout()
        '
        'Groupbox_TaskTime
        '
        Me.Groupbox_TaskTime.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Groupbox_TaskTime.Controls.Add(Me.ProgressBar1)
        Me.Groupbox_TaskTime.Font = New System.Drawing.Font("Microsoft Sans Serif", 14.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.Groupbox_TaskTime.ForeColor = System.Drawing.SystemColors.WindowText
        Me.Groupbox_TaskTime.Location = New System.Drawing.Point(7, 6)
        Me.Groupbox_TaskTime.Margin = New System.Windows.Forms.Padding(2)
        Me.Groupbox_TaskTime.Name = "Groupbox_TaskTime"
        Me.Groupbox_TaskTime.Size = New System.Drawing.Size(689, 60)
        Me.Groupbox_TaskTime.TabIndex = 2
        Me.Groupbox_TaskTime.TabStop = False
        Me.Groupbox_TaskTime.Text = "EuroSound Task Time Remaining:"
        '
        'ProgressBar1
        '
        Me.ProgressBar1.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ProgressBar1.Location = New System.Drawing.Point(5, 29)
        Me.ProgressBar1.Name = "ProgressBar1"
        Me.ProgressBar1.Size = New System.Drawing.Size(679, 23)
        Me.ProgressBar1.TabIndex = 0
        '
        'Frm_TimerForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(703, 72)
        Me.Controls.Add(Me.Groupbox_TaskTime)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.MaximizeBox = False
        Me.Name = "Frm_TimerForm"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "EuroSound Timer Form"
        Me.Groupbox_TaskTime.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

    Protected Friend WithEvents Groupbox_TaskTime As GroupBox
    Protected Friend WithEvents ProgressBar1 As ProgressBar
End Class
