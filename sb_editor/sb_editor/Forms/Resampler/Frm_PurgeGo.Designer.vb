﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Frm_PurgeGo
    Inherits Frm_TimerForm

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
        Me.BackgroundWorker = New System.ComponentModel.BackgroundWorker()
        Me.Groupbox_TaskTime.SuspendLayout()
        Me.SuspendLayout()
        '
        'BackgroundWorker
        '
        Me.BackgroundWorker.WorkerReportsProgress = True
        '
        'Frm_PurgeGo
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(839, 92)
        Me.Cursor = System.Windows.Forms.Cursors.Default
        Me.Name = "Frm_PurgeGo"
        Me.ShowInTaskbar = False
        Me.Text = "Frm_PurgeGo"
        Me.Groupbox_TaskTime.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents BackgroundWorker As System.ComponentModel.BackgroundWorker
End Class
