Dim qtApp 'As QuickTest.Application ' Declare the Application object variable
Dim qtTest 'As QuickTest.Test ' Declare a Test object variable
Dim qtResultsOpt 'As QuickTest.RunResultsOptions ' Declare a Run Results Options object variable
Dim qtAutoExportResultsOpts 'As QuickTest.AutoExportReportConfigOptions ' Declare the Automatically Export Report Configuration Options object variable
Dim strStatus
Dim strTestName
Dim strLastError
	
Function test_start(a, b)	
	Set qtApp=createobject("QuickTest.Application") 
	 If Not qtApp.Launched Then 
	 qtApp.Launch
	 End If
	qtApp.visible = False ' qt запускается в тихом режиме
	
	' Set UFT run options
	qtApp.Options.Run.RunMode = "Fast"
	qtApp.Options.Run.ViewResults = False
	qtApp.Options.Run.EnableRdp = True
	qtApp.Open a, True ' ' Open the test in read-only mode
	
	' Configure the Web application to use with this test
    qtApp.Test.Settings.Launchers("Web").Active = True
    qtApp.Test.Settings.Launchers("Web").SetLab "LocalBrowser"
    qtApp.Test.Settings.Launchers("Web").Browser = "IE"
    qtApp.Test.Settings.Launchers("Web").Address = AddressHost
    qtApp.Test.Settings.Launchers("Web").CloseOnExit = True
    qtApp.Test.Settings.Launchers("Web").RuntimeParameterization = False
    qtApp.visible=False ' qt запускается в тихом режиме
	
	' set run settings for the test
	Set qtTest = qtApp.Test 
	qtTest.Settings.Run.OnError = "Stop"
	
	' Настраиваем папку выгрузки результатов
	Set qtResultsOpt = CreateObject("QuickTest.RunResultsOptions") '
	qtResultsOpt.ResultsLocation = b 
	
	' Set options for automatic export of run results at the end of every run session
	Set qtAutoExportResultsOpts = qtApp.Options.Run.AutoExportReportConfig
	qtAutoExportResultsOpts.AutoExportResults = False ' Instruct qt to automatically export the run results at the end of each run session
	qtAutoExportResultsOpts.StepDetailsReport = True ' Instruct qt to automatically export the step details part of the run results at the end of each run session
	qtAutoExportResultsOpts.DataTableReport = True ' Instruct qt to automatically export the data table part of the run results at the end of each run session
	qtAutoExportResultsOpts.LogTrackingReport = True ' Instruct qt to automatically export the log tracking part of the run results at the end of each run session
	qtAutoExportResultsOpts.ScreenRecorderReport = True ' Instruct qt to automatically export the screen recorder part of the run results at the end of each run session
	qtAutoExportResultsOpts.SystemMonitorReport = True ' Instruct qt not to automatically export the system monitor part of the run results at the end of each run session
	qtAutoExportResultsOpts.ExportLocation = "z:\results\report_tmp" 'Instruct qt to automatically export the run results to the Desktop at the end of each run session
	qtAutoExportResultsOpts.UserDefinedXSL = False  ' Specify the customized XSL file when exporting the run results data
	qtAutoExportResultsOpts.StepDetailsReportFormat = "Detailed"  ' Instruct qt to use a customized XSL file when exporting the run results data "Detailed" or "Short"
	qtAutoExportResultsOpts.ExportForFailedRunsOnly = False ' Instruct qt to automatically export run results only for failed runs
	
	qtTest.Run qtResultsOpt ' Запускаем тест 
	strStatus = qtTest.LastRunResults.Status 'Записываем Статус теста
	strLastError = qtTest.LastRunResults.LastError 'Записываем ошибку теста
	strTestName = qtTest.Name 'Записываем Имя теста
	qtTest.Close 'Закрываем выполненый тест
	qtApp.Quit 'Закрываем UFT
	
	' WScript.Echo(strTestName & "=" & strStatus & "-" & strLastError) 'Выводим в лог статус теста
	
	Set qtResultsOpt = Nothing ' Release the Run Results Options object
	Set qtTest = Nothing ' Release the Test object
	Set qtApp = Nothing ' Release the Application object
	Set qtAutoExportResultsOpts = Nothing ' Release the Automatically Export Report Configuration Options object
	Set qtAutoExportResultsOpts = Nothing ' Release the Automatically Export Report Configuration Options object
End Function
