exec("./resources/wrenchDlgLimited.gui");

$LWrench::Connected = false;

// limited wrench dialog
function clientCmdShowLWrenchDlg(%ownerName) {
	Canvas.pushDialog(lWrenchDlg);
	
	lWrench_Window.setText("Limited Wrench - " @ %ownerName);
}

// data
function clientCmdLWrenchLight(%name, %id) {
	if(%name !$= "")
		lWrench_Lights.add(%name, %id);
}

function clientCmdLWrenchEmitter(%name, %id) {
	if(%name !$= "")
		lWrench_Emitters.add(%name, %id);
}

// current brick stuff
function clientCmdLWrenchCurLight(%id) {
	if(lWrenchLock_Lights.getValue()) {
		return;
	}
	
	lWrench_Lights.setSelected(%id);
}

function clientCmdLWrenchCurEmitter(%id) {
	if(lWrenchLock_Emitters.getValue()) {
		return;
	}
	
	lWrench_Emitters.setSelected(%id);
}

function clientCmdLWrenchCurEmitterDir(%id) {
	if(lWrenchLock_EmitterDir.getValue()) {
		return;
	}
	
	lWrench_EmitterDir0.setValue(0);
	lWrench_EmitterDir1.setValue(0);
	lWrench_EmitterDir2.setValue(0);
	lWrench_EmitterDir3.setValue(0);
	lWrench_EmitterDir4.setValue(0);
	lWrench_EmitterDir5.setValue(0);
	
	if(%id == 0) {
		lWrench_EmitterDir0.setValue(1);
	}
	else if(%id == 1) {
		lWrench_EmitterDir1.setValue(1);
	}
	else if(%id == 2) {
		lWrench_EmitterDir2.setValue(1);
	}
	else if(%id == 3) {
		lWrench_EmitterDir3.setValue(1);
	}
	else if(%id == 4) {
		lWrench_EmitterDir4.setValue(1);
	}
	else if(%id == 5) {
		lWrench_EmitterDir5.setValue(1);
	}
}

function clientCmdLWrenchCurRendering(%id) {
	if(lWrenchLock_Rendering.getValue()) {
		return;
	}
	
	lWrench_Rendering.setValue(%id);
}

function clientCmdLWrenchCurName(%name) {
	if(lWrenchLock_Name.getValue()) {
		return;
	}
	
	lWrench_Name.setValue(%name);
}

function getLWrenchEmitterDir() {
	if(lWrench_EmitterDir0.getValue()) {
		return 0;
	}
	else if(lWrench_EmitterDir1.getValue()) {
		return 1;
	}
	else if(lWrench_EmitterDir2.getValue()) {
		return 2;
	}
	else if(lWrench_EmitterDir3.getValue()) {
		return 3;
	}
	else if(lWrench_EmitterDir4.getValue()) {
		return 4;
	}
	else if(lWrench_EmitterDir5.getValue()) {
		return 5;
	}
}

function lWrenchDlg::onWake() {
	if(!$LWrench::Connected) {
		lWrench_Lights.clear();
		lWrench_Emitters.clear();
		
		lWrench_Lights.add("NONE", -1);
		lWrench_Emitters.add("NONE", -1);
		
		commandToServer('sendLWrenchData');
		
		$LWrench::Connected = true;
	}
}

function clientCmdLWrenchCheck() {
	commandToServer('enableLWrench');
}

function lWrenchDlg::send() {
	Canvas.popDialog(lWrenchDlg);
	
	// pretty much do the same thing badspot's wrench does.
	// take the selections and send them to the server.
	commandToServer('LWrenchNewLight', lWrench_Lights.getSelected());
	commandToServer('LWrenchNewEmitter', lWrench_Emitters.getSelected());
	commandToServer('LWrenchNewEmitterDir', getLWrenchEmitterDir());
	commandToServer('LWrenchNewRendering', lWrench_Rendering.getValue());
	commandToServer('LWrenchNewName', lWrench_Name.getValue());
}

if(isPackage(lWrenchClient))
	deactivatePackage(lWrenchClient);

package lWrenchClient {
	function disconnectedCleanup()
	{
		$LWrench::Connected = false;
		
		lWrench_Lights.clear();
		lWrench_Emitters.clear();
		
		Parent::disconnectedCleanup();
	}
};

activatePackage(lWrenchClient);