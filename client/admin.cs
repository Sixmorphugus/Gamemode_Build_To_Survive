exec("./resources/BTSAdminGUI.gui");

$BTSAdmin::Connected = false;

function clientCmdBTSAOpen() {
	Canvas.pushDialog(BTSAdminGui);
}

function clientCmdListDisaster(%name, %id) {
	BTSDisasterList.addRow(%id, %name);
}

function clientCmdDPrefInfo(%prepTime, %wrenchMode, %destructionMode) {
	BTSBuildTime.setValue(%prepTime);
	BTSWrenchMode.setSelected(%wrenchMode);
	BTSDestructionMode.setSelected(%destructionMode);
}

function clientCmdDNextInfo(%next) {
	BTSANextUp.setText("Next up: " @ %next);
}

// gui
function BTSAdminGui::onWake(%this) {
	BTSDisasterList.clear();
	BTSDisasterTypeList.clear();
	BTSWrenchMode.clear();
	BTSDestructionMode.clear();
	
	BTSWrenchMode.add("Full", 2);
	BTSWrenchMode.add("Limited", 1);
	BTSWrenchMode.add("Disabled", 0);
	
	BTSDisasterTypeList.addRow(0, "Common Disasters");
	BTSDisasterTypeList.addRow(1, "Major Disasters");
	
	BTSDestructionMode.add("On", 1);
	BTSDestructionMode.add("Off", 0);

	commandToServer('requestDisasterList', 0);
}

function BTSAdminGui::selectDisasterType(%this) {
	BTSDisasterList.clear();
	commandToServer('requestDisasterList', BTSDisasterTypeList.getSelectedId());
}

function BTSAdminGui::done(%this) {
	Canvas.popDialog(BTSAdminGUI);
	commandToServer('NewDPrefs', BTSBuildTime.getValue(), BTSWrenchMode.getSelected(), BTSDestructionMode.getSelected());
}

function BTSAdminGui::forceDisaster(%this) {
	if(BTSDisasterList.getSelectedId() != -1) {
		//Canvas.popDialog(BTSAdminGUI);
		commandToServer('forceDisaster', BTSDisasterList.getSelectedId());
	}
}