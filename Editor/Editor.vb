﻿Public Class Editor

    Private WithEvents m_controller As EditorController
    Private m_elementEditors As Dictionary(Of String, ElementEditor)
    Private m_currentEditor As ElementEditor
    Private m_menu As AxeSoftware.Quest.Controls.Menu
    Private m_filename As String
    Private m_currentElement As String
    Private m_codeView As Boolean

    Public Event AddToRecent(filename As String, name As String)
    Public Event Close()
    Public Event Play(filename As String)

    Public Function Initialise(ByRef filename As String) As Boolean
        m_filename = filename
        m_controller = New EditorController()
        InitialiseEditorControlsList()
        Dim ok As Boolean = m_controller.Initialise(filename)
        If ok Then
            SetUpTree()
            SetUpToolbar()
            SetUpEditors()
            RaiseEvent AddToRecent(filename, m_controller.GameName)
            ctlTree.SetSelectedItem("game")
            ctlTree.FocusOnTree()
        End If

        Return ok
    End Function

    Private Sub InitialiseEditorControlsList()
        For Each t As Type In AxeSoftware.Utility.Classes.GetImplementations(System.Reflection.Assembly.GetExecutingAssembly(), GetType(IElementEditorControl))
            Dim controlType As ControlTypeAttribute = DirectCast(Attribute.GetCustomAttribute(t, GetType(ControlTypeAttribute)), ControlTypeAttribute)
            If Not controlType Is Nothing Then
                m_controller.AddControlType(controlType.ControlType, t)
            End If
        Next
    End Sub

    Public Sub SetMenu(menu As AxeSoftware.Quest.Controls.Menu)
        m_menu = menu
        menu.AddMenuClickHandler("save", AddressOf Save)
        menu.AddMenuClickHandler("saveas", AddressOf SaveAs)
        menu.AddMenuClickHandler("undo", AddressOf Undo)
        menu.AddMenuClickHandler("redo", AddressOf Redo)
        menu.AddMenuClickHandler("addobject", AddressOf AddNewObject)
        menu.AddMenuClickHandler("addroom", AddressOf AddNewRoom)
        menu.AddMenuClickHandler("addexit", AddressOf AddNewExit)
        menu.AddMenuClickHandler("addverb", AddressOf AddNewVerb)
        menu.AddMenuClickHandler("addcommand", AddressOf AddNewCommand)
        menu.AddMenuClickHandler("addfunction", AddressOf AddNewFunction)
        menu.AddMenuClickHandler("addlibrary", AddressOf AddNewLibrary)
        menu.AddMenuClickHandler("addimpliedtype", AddressOf AddNewImpliedType)
        menu.AddMenuClickHandler("addtemplate", AddressOf AddNewTemplate)
        menu.AddMenuClickHandler("adddynamictemplate", AddressOf AddNewDynamicTemplate)
        menu.AddMenuClickHandler("adddelegate", AddressOf AddNewDelegate)
        menu.AddMenuClickHandler("addobjecttype", AddressOf AddNewObjectType)
        menu.AddMenuClickHandler("addeditor", AddressOf AddNewEditor)
        menu.AddMenuClickHandler("play", AddressOf PlayGame)
        menu.AddMenuClickHandler("close", AddressOf CloseEditor)
        menu.AddMenuClickHandler("cut", AddressOf Cut)
        menu.AddMenuClickHandler("copy", AddressOf Copy)
        menu.AddMenuClickHandler("paste", AddressOf Paste)
        menu.AddMenuClickHandler("delete", AddressOf Delete)
    End Sub

    Private Sub SetUpToolbar()
        ctlToolbar.ResetToolbar()
        ctlToolbar.AddButtonHandler("save", AddressOf Save)
        ctlToolbar.AddButtonHandler("undo", AddressOf Undo)
        ctlToolbar.AddButtonHandler("redo", AddressOf Redo)
        ctlToolbar.AddButtonHandler("addobject", AddressOf AddNewObject)
        ctlToolbar.AddButtonHandler("addroom", AddressOf AddNewRoom)
        ctlToolbar.AddButtonHandler("play", AddressOf PlayGame)
        ctlToolbar.AddButtonHandler("cut", AddressOf Cut)
        ctlToolbar.AddButtonHandler("copy", AddressOf Copy)
        ctlToolbar.AddButtonHandler("paste", AddressOf Paste)
        ctlToolbar.AddButtonHandler("delete", AddressOf Delete)
        ctlToolbar.AddButtonHandler("code", AddressOf ToggleCodeView)
    End Sub

    Private Sub SetUpTree()
        ctlTree.SetAvailableFilters(m_controller.AvailableFilters)
        ctlTree.SetCanDragDelegate(AddressOf m_controller.CanMoveElement)
        ctlTree.SetDoDragDelegate(AddressOf m_controller.MoveElement)
        ctlTree.CollapseAdvancedNode()
        ctlTree.ScrollToTop()

        ctlTree.AddMenuClickHandler("addobject", AddressOf AddNewObject)
        ctlTree.AddMenuClickHandler("addroom", AddressOf AddNewRoom)
        ctlTree.AddMenuClickHandler("addexit", AddressOf AddNewExit)
        ctlTree.AddMenuClickHandler("addverb", AddressOf AddNewVerb)
        ctlTree.AddMenuClickHandler("addcommand", AddressOf AddNewCommand)
        ctlTree.AddMenuClickHandler("addfunction", AddressOf AddNewFunction)
        ctlTree.AddMenuClickHandler("addlibrary", AddressOf AddNewLibrary)
        ctlTree.AddMenuClickHandler("addimpliedtype", AddressOf AddNewImpliedType)
        ctlTree.AddMenuClickHandler("addtemplate", AddressOf AddNewTemplate)
        ctlTree.AddMenuClickHandler("adddynamictemplate", AddressOf AddNewDynamicTemplate)
        ctlTree.AddMenuClickHandler("adddelegate", AddressOf AddNewDelegate)
        ctlTree.AddMenuClickHandler("addobjecttype", AddressOf AddNewObjectType)
        ctlTree.AddMenuClickHandler("addeditor", AddressOf AddNewEditor)
    End Sub

    Private Sub SetUpEditors()
        m_elementEditors = New Dictionary(Of String, ElementEditor)

        For Each editor As String In m_controller.GetAllEditorNames()
            AddEditor(editor)
        Next
    End Sub

    Private Sub AddEditor(name As String)
        ' Get an EditorDefinition from the EditorController, then pass it in to the ElementEditor so it can initialise its
        ' tabs and subcontrols.
        Dim editor As ElementEditor
        editor = New ElementEditor
        editor.Initialise(m_controller, m_controller.GetEditorDefinition(name))
        editor.Visible = False
        editor.Parent = pnlContent
        editor.Dock = DockStyle.Fill
        AddHandler editor.Dirty, AddressOf Editor_Dirty
        m_elementEditors.Add(name, editor)
    End Sub

    Private Sub Editor_Dirty(sender As Object, args As DataModifiedEventArgs)
        ctlToolbar.EnableUndo()
        ' TO DO: Set status saying game not saved
    End Sub

    Private Sub m_controller_AddedNode(key As String, text As String, parent As String, foreColor As System.Drawing.Color?, backColor As System.Drawing.Color?) Handles m_controller.AddedNode
        ctlTree.AddNode(key, text, parent, foreColor, backColor)
    End Sub

    Private Sub m_controller_RemovedNode(key As String) Handles m_controller.RemovedNode
        ctlTree.RemoveNode(key)
    End Sub

    Private Sub m_controller_RenamedNode(oldName As String, newName As String) Handles m_controller.RenamedNode
        If m_currentElement = oldName Then
            m_currentElement = newName
            RefreshCurrentElement()
        End If
        ctlTree.RenameNode(oldName, newName)
        ctlToolbar.RenameHistory(oldName, newName)
    End Sub

    Private Sub m_controller_RetitledNode(key As String, newTitle As String) Handles m_controller.RetitledNode
        If (m_currentElement = key) Then
            lblHeader.Text = newTitle
        End If
        ctlTree.RetitleNode(key, newTitle)
        ctlToolbar.RetitleHistory(key, newTitle)
    End Sub

    Private Sub m_controller_BeginTreeUpdate() Handles m_controller.BeginTreeUpdate
        ctlTree.BeginUpdate()
    End Sub

    Private Sub m_controller_ClearTree() Handles m_controller.ClearTree
        ctlTree.Clear()
    End Sub

    Private Sub m_controller_ElementUpdated(sender As Object, e As EditorController.ElementUpdatedEventArgs) Handles m_controller.ElementUpdated
        If e.Element = m_currentElement Then
            m_currentEditor.UpdateField(e.Attribute, e.NewValue, e.IsUndo)
        End If
    End Sub

    Private Sub m_controller_ElementRefreshed(sender As Object, e As EditorController.ElementRefreshedEventArgs) Handles m_controller.ElementRefreshed
        If e.Element = m_currentElement Then
            RefreshCurrentElement()
        End If
    End Sub

    Private Sub RefreshCurrentElement()
        m_currentEditor.Populate(m_controller.GetEditorData(m_currentElement))
    End Sub

    Private Sub m_controller_EndTreeUpdate() Handles m_controller.EndTreeUpdate
        ctlTree.EndUpdate()
    End Sub

    Private Sub m_controller_UndoListUpdated(sender As Object, e As EditorController.UpdateUndoListEventArgs) Handles m_controller.UndoListUpdated
        ctlToolbar.UpdateUndoMenu(e.UndoList)
    End Sub

    Private Sub m_controller_RedoListUpdated(sender As Object, e As EditorController.UpdateUndoListEventArgs) Handles m_controller.RedoListUpdated
        ctlToolbar.UpdateRedoMenu(e.UndoList)
    End Sub

    Private Sub m_controller_ShowMessage(message As String) Handles m_controller.ShowMessage
        System.Windows.Forms.MessageBox.Show(message)
    End Sub

    Private Sub ctlTree_FiltersUpdated() Handles ctlTree.FiltersUpdated
        m_controller.UpdateFilterOptions(ctlTree.FilterSettings)
    End Sub

    Private Sub ctlTree_TreeGotFocus() Handles ctlTree.TreeGotFocus
        SetMenuShortcutKeys()
    End Sub

    Private Sub ctlTree_TreeLostFocus() Handles ctlTree.TreeLostFocus
        UnsetMenuShortcutKeys()
    End Sub

    Private Sub ctlTree_SelectionChanged(key As String) Handles ctlTree.SelectionChanged
        ctlToolbar.AddHistory(key, m_controller.GetDisplayName(key))
        ShowEditor(key)
    End Sub

    Private Sub SetMenuShortcutKeys()
        m_menu.SetShortcut("cut", Keys.Control Or Keys.X)
        m_menu.SetShortcut("copy", Keys.Control Or Keys.C)
        m_menu.SetShortcut("paste", Keys.Control Or Keys.V)
        m_menu.SetShortcut("delete", Keys.Delete)
    End Sub

    Private Sub UnsetMenuShortcutKeys()
        m_menu.SetShortcut("cut", Keys.None)
        m_menu.SetShortcut("copy", Keys.None)
        m_menu.SetShortcut("paste", Keys.None)
        m_menu.SetShortcut("delete", Keys.None)
    End Sub

    Private Sub ShowEditor(key As String)

        Dim editorName As String = m_controller.GetElementEditorName(key)
        If editorName Is Nothing Then
            If m_currentEditor IsNot Nothing Then
                m_currentEditor.Visible = False
            End If

            m_currentEditor = Nothing
        Else
            Dim nextEditor As ElementEditor = m_elementEditors(editorName)

            If Not m_currentEditor Is Nothing Then
                If Not m_currentEditor.Equals(nextEditor) Then
                    m_currentEditor.Visible = False
                End If
            End If

            m_currentEditor = nextEditor

            m_currentElement = key
            m_currentEditor.Populate(m_controller.GetEditorData(key))
            nextEditor.Visible = True
        End If

        lblHeader.Text = m_controller.GetDisplayName(key)
    End Sub

    Private Function Save() As Boolean
        If (m_filename.Length = 0) Then
            Return SaveAs()
        Else
            Return Save(m_filename)
        End If
    End Function

    Private Function SaveAs() As Boolean
        ctlSaveFile.FileName = m_filename
        If ctlSaveFile.ShowDialog() = DialogResult.OK Then
            m_filename = ctlSaveFile.FileName
            Return Save(m_filename)
        End If
        Return False
    End Function

    Private Function Save(filename As String) As Boolean
        Try
            If m_codeView Then
                ctlTextEditor.SaveFile(filename)
            Else
                If Not m_currentEditor Is Nothing Then
                    m_currentEditor.SaveData()
                End If
                System.IO.File.WriteAllText(filename, m_controller.Save())
            End If
            Return True
        Catch ex As Exception
            MsgBox("Unable to save the file due to the following error:" + Environment.NewLine + Environment.NewLine + ex.Message, MsgBoxStyle.Critical)
            Return False
        End Try
    End Function

    Private Sub ctlToolbar_HistoryClicked(Key As String) Handles ctlToolbar.HistoryClicked
        ctlTree.SetSelectedItemNoEvent(Key)
        ShowEditor(Key)
    End Sub

    Private Sub Undo()
        If m_codeView Then
            ctlTextEditor.Undo()
        Else
            If Not m_currentEditor Is Nothing Then
                m_currentEditor.SaveData()
                m_controller.Undo()
            End If
        End If
    End Sub

    Private Sub Redo()
        If m_codeView Then
            ctlTextEditor.Redo()
        Else
            If Not m_currentEditor Is Nothing Then
                m_currentEditor.SaveData()
                m_controller.Redo()
            End If
        End If
    End Sub

    Private Sub ctlToolbar_SaveCurrentEditor() Handles ctlToolbar.SaveCurrentEditor
        If Not m_currentEditor Is Nothing Then
            m_currentEditor.SaveData()
        End If
    End Sub

    Private Sub ctlToolbar_UndoClicked(level As Integer) Handles ctlToolbar.UndoClicked
        m_controller.Undo(level)
    End Sub

    Private Sub ctlToolbar_RedoClicked(level As Integer) Handles ctlToolbar.RedoClicked
        m_controller.Redo(level)
    End Sub

    Private Sub ctlToolbar_UndoEnabled(enabled As Boolean) Handles ctlToolbar.UndoEnabled
        m_menu.MenuEnabled("undo") = enabled
    End Sub

    Private Sub ctlToolbar_RedoEnabled(enabled As Boolean) Handles ctlToolbar.RedoEnabled
        m_menu.MenuEnabled("redo") = enabled
    End Sub

    Private Function GetParentForCurrentSelection() As String
        If m_controller.GetElementType(ctlTree.SelectedItem) = "object" AndAlso m_controller.GetObjectType(ctlTree.SelectedItem) = "object" Then
            Return ctlTree.SelectedItem
        Else
            Return Nothing
        End If
    End Function

    Private Sub AddNewElement(typeName As String, action As Action(Of String))
        Dim result = PopupEditors.EditString(String.Format("Please enter a name for the new {0}", typeName), "")
        If result.Cancelled Then Return
        If Not ValidateInput(result.Result) Then Return

        action(result.Result)
        ctlTree.SetSelectedItem(result.Result)
    End Sub

    Private Sub AddNewObject()

        Dim possibleParents = m_controller.GetPossibleNewObjectParentsForCurrentSelection(ctlTree.SelectedItem)
        Const prompt As String = "Please enter a name for the new object"
        Const noParent As String = "(none)"

        If possibleParents Is Nothing Then
            Dim result = PopupEditors.EditString(prompt, "")
            If result.Cancelled Then Return
            If Not ValidateInput(result.Result) Then Return

            m_controller.CreateNewObject(result.Result, Nothing)
            ctlTree.SetSelectedItem(result.Result)
        Else
            Dim parentOptions As New List(Of String)
            parentOptions.Add(noParent)
            parentOptions.AddRange(possibleParents)

            Dim result = PopupEditors.EditStringWithDropdown(prompt, "", "Parent", parentOptions, ctlTree.SelectedItem)
            If result.Cancelled Then Return
            If Not ValidateInput(result.Result) Then Return

            Dim parent = result.ListResult
            If parent = noParent Then parent = Nothing

            m_controller.CreateNewObject(result.Result, parent)
            ctlTree.SetSelectedItem(result.Result)
        End If

    End Sub

    Private Sub AddNewRoom()
        Dim result = PopupEditors.EditString("Please enter a name for the new room", "")
        If result.Cancelled Then Return
        If Not ValidateInput(result.Result) Then Return

        m_controller.CreateNewRoom(result.Result, Nothing)
        ctlTree.SetSelectedItem(result.Result)
    End Sub

    Private Sub AddNewExit()
        Dim newExit = m_controller.CreateNewExit(GetParentForCurrentSelection())
        ctlTree.SetSelectedItem(newExit)
    End Sub

    Private Sub AddNewVerb()
        Dim newVerb = m_controller.CreateNewVerb(GetParentForCurrentSelection(), True)
        ctlTree.SetSelectedItem(newVerb)
    End Sub

    Private Sub AddNewCommand()
        Dim newCommand = m_controller.CreateNewCommand(GetParentForCurrentSelection())
        ctlTree.SetSelectedItem(newCommand)
    End Sub

    Private Sub AddNewFunction()
        AddNewElement("function", AddressOf m_controller.CreateNewFunction)
    End Sub

    Private Sub AddNewLibrary()
        MsgBox("Not yet implemented")
    End Sub

    Private Sub AddNewImpliedType()
        MsgBox("Not yet implemented")
    End Sub

    Private Sub AddNewTemplate()
        MsgBox("Not yet implemented")
    End Sub

    Private Sub AddNewDynamicTemplate()
        MsgBox("Not yet implemented")
    End Sub

    Private Sub AddNewDelegate()
        MsgBox("Not yet implemented")
    End Sub

    Private Sub AddNewObjectType()
        AddNewElement("object type", AddressOf m_controller.CreateNewType)
    End Sub

    Private Sub AddNewEditor()
        MsgBox("Not yet implemented")
    End Sub

    Private Function ValidateInput(input As String) As Boolean
        Dim result = m_controller.CanAdd(input)
        If result.Valid Then Return True

        MsgBox(PopupEditors.GetError(result.Message, input), MsgBoxStyle.Exclamation, "Unable to add element")
        Return False
    End Function

    Public Function CreateNewGame() As String
        Dim templates As Dictionary(Of String, String) = GetAvailableTemplates()
        Dim newGameWindow As New NewGameWindow
        newGameWindow.SetAvailableTemplates(templates)
        newGameWindow.ShowDialog()

        If newGameWindow.Cancelled Then Return Nothing

        Dim filename = newGameWindow.txtFilename.Text
        Dim folder = System.IO.Path.GetDirectoryName(filename)
        If Not System.IO.Directory.Exists(folder) Then
            System.IO.Directory.CreateDirectory(folder)
        End If

        Dim templateText = System.IO.File.ReadAllText(templates(newGameWindow.lstTemplate.Text))
        Dim initialFileText = templateText.Replace("$NAME$", newGameWindow.txtGameName.Text)

        System.IO.File.WriteAllText(filename, initialFileText)

        Return filename
    End Function

    Public Function GetAvailableTemplates() As Dictionary(Of String, String)
        Dim templates As New Dictionary(Of String, String)

        Dim folder As String = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().CodeBase)
        folder = folder.Substring(6) ' remove initial "file://"

        For Each file In System.IO.Directory.GetFiles(folder, "*.template", System.IO.SearchOption.AllDirectories)
            Dim key As String = System.IO.Path.GetFileNameWithoutExtension(file)
            If Not templates.ContainsKey(key) Then
                templates.Add(key, file)
            End If
        Next

        Return templates
    End Function

    Private Sub PlayGame()
        ' TO DO: Current game must be saved and up to date i.e. non-dirty
        RaiseEvent Play(m_filename)
    End Sub

    Private Sub CloseEditor()
        RaiseEvent Close()
    End Sub

    Private Sub Cut()
        If m_codeView Then
            ctlTextEditor.Cut()
        Else
            MsgBox("Cut not yet implemented")
        End If
    End Sub

    Private Sub Copy()
        If m_codeView Then
            ctlTextEditor.Copy()
        Else
            MsgBox("Copy not yet implemented")
        End If
    End Sub

    Private Sub Paste()
        If m_codeView Then
            ctlTextEditor.Paste()
        Else
            MsgBox("Paste not yet implemented")
        End If
    End Sub

    Private Sub Delete()
        m_controller.DeleteElement(ctlTree.SelectedItem, True)
    End Sub

    Private Sub ToggleCodeView()
        Dim saveOk As Boolean = Save()
        If Not saveOk Then Return

        DisplayCodeView(Not m_codeView)

        If m_codeView Then
            ctlTextEditor.LoadFile(m_filename)
            ctlTextEditor.Focus()
        Else
            If ctlTextEditor.TextWasSaved Then
                ' file was changed in the text editor, so reload it
                Dim ok As Boolean = Initialise(m_filename)
                If Not ok Then
                    ' Couldn't reload the file due to an error, so show code view again
                    DisplayCodeView(True)
                End If
            End If
        End If
    End Sub

    Private Sub DisplayCodeView(codeView As Boolean)
        m_codeView = codeView
        ctlToolbar.SetToggle("code", codeView)
        ctlTextEditor.Visible = codeView
        splitMain.Visible = Not codeView
        ctlToolbar.CodeView = codeView
    End Sub

    Private Sub ctlTextEditor_UndoRedoEnabledUpdated(undoEnabled As Boolean, redoEnabled As Boolean) Handles ctlTextEditor.UndoRedoEnabledUpdated
        ctlToolbar.UndoButtonEnabled = undoEnabled
        ctlToolbar.RedoButtonEnabled = redoEnabled
    End Sub

    Private Sub m_controller_RequestAddElement(elementType As String, objectType As String) Handles m_controller.RequestAddElement
        Select Case elementType
            Case "object"
                Select Case objectType
                    Case "object"
                        AddNewObject()
                    Case "exit"
                        AddNewExit()
                    Case "command"
                        AddNewCommand()
                    Case Else
                        Throw New ArgumentOutOfRangeException
                End Select
            Case Else
                Throw New ArgumentOutOfRangeException
        End Select
    End Sub

    Private Sub m_controller_RequestEdit(key As String) Handles m_controller.RequestEdit
        ctlTree.SetSelectedItemNoEvent(key)
        ShowEditor(key)
    End Sub
End Class
