function serverCmdAdmin(%client) {
	if(%client.isAdmin) {
		commandToClient(%client, 'BTSAOpen');
	} else {
		MessageClient(%client, '', "You are not an admin.");
	}
}

function serverCmdRequestDisasterList(%client, %typ) {
	if(!%client.isAdmin)
		return; // fuck off m8
	
	if(%typ $= "")
		%typ = 0;
	
	for(%i = 0; %i < DisastersList.getCount(); %i++) {
		%disaster = DisastersList.getObject(%i);
		
		if(%disaster.type == %typ)
			commandToClient(%client, 'listDisaster', %disaster.name, %i);
	}
	
	commandToClient(%client, 'DNextInfo', $Disasters::CurrentDisaster.name);
	commandToClient(%client, 'DPrefInfo', $Disasters::Prep, $Disasters::Wrench, $Disasters::Destruction);
	
	export("$Disasters::*", "config/server/disastersPrefs.cs", false);
}

function serverCmdNewDPrefs(%client, %prepTime, %wrenchMode, %destructionMode) {
	if(!%client.isAdmin)
		return; // fuck off m8
	
	if($Disasters::Prep != %prepTime) {
		messageAll('MsgAdminForce', "\c6Disaster prep time is now \c0" @ getTimeString(%prepTime) @ "\c6.");
	}
	
	%toolreset = false;
	
	if($Disasters::Wrench != %wrenchMode) {
		%info = "Disabled";
		
		if(%wrenchMode $= "1")
			%info = "Limited";
		else if(%wrenchMode $= "2")
			%info = "Unrestricted";
		
		messageAll('MsgAdminForce', "\c6Wrenches are now \c0" @ %info @ "\c6.");
		
		%mg = $DefaultMiniGame;
	
		%toolreset = true;
	}
	
	if($Disasters::Destruction != %destructionMode) {
		%info = "Off";
		
		if(%destructionMode $= "1")
			%info = "On";
		
		messageAll('MsgAdminForce', "\c6Destruction is now \c0" @ %info @ "\c6.");
		
		%mg = $DefaultMiniGame;
	}
	
	$Disasters::Prep = %prepTime;
	$Disasters::Wrench = %wrenchMode;
	$Disasters::Destruction = %destructionMode;
	
	// if wrench setting has changed, replace everyone's tools
	if(%toolreset) {
		for(%i=0;%i<%mg.numMembers;%i++)
		{
			%client = %mg.member[%i];

			%client.player.setShapeNameColor("1.0 1.0 1.0");
			
			// take tools
			%client.player.clearTools();
			
			// give building tools
			%client.giveTools();
		}
	}
}