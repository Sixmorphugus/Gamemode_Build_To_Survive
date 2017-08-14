if(isFile("config/server/disastersPrefs.cs")) {
	exec("config/server/disastersPrefs.cs");
}

if($Disasters::RoundLimit $= "")
   $Disasters::RoundLimit = 6;

if($Disasters::TimeLimit $= "")
   $Disasters::TimeLimit = 180;

if($Disasters::Prep $= "")
	$Disasters::Prep = 180;

if($Disasters::Destruction $= "")
	$Disasters::Destruction = "1";

if($Disasters::Running $= "")
	$Disasters::Running = false;

if($Disasters::Round $= "")
	$Disasters::Round = 0;

if($Disasters::Wrench $= "")
	$Disasters::Wrench = "0";

if($Disasters::Record $= "")
	$Disasters::Record = 0;

if($Disasters::RecordName $= "")
	$Disasters::RecordName = "Nobody";

if(isPackage(DisastersPackage))
	deactivatePackage(DisastersPackage);

function createDisasterList() {
    if(isObject(DisastersList))
		DisastersList.clear();
	else
		new ScriptGroup(DisastersList) { name = "DisastersList"; };
       
	// execute all BTSe extension mods.
	echo("LOADING EXTENSIONS");
    %file = findFirstFile("Add-Ons/BTSe_*/server.cs");
    
    while(%file !$= "")
	{
        exec(%file);

	    %file = findNextFile("Add-Ons/BTSe_*/server.cs");
	}
}

function registerDisaster(%name, %roundTime, %startFunc, %stopFunc, %type) {
	if(%type $= "")
		%type = 0; // Common disaster
	
	%data = new ScriptObject()
	{
		name = %name;
		roundTime = %roundTime;
		startFunction = %startFunc;
		
		hasStopFunction = %stopFunc $= "" ? false : true;
		stopFunction = %stopFunc;
		
		type = %type;
	};
	DisastersList.add(%data);
}

function pickRandomDisaster() {
    %disastersCount = DisastersList.getCount()-1;
    
	%majorChance = 0.01;
	%disasterType = getRandom(0, (1 / %majorChance)-1) ? 0 : 1;
	
	echo(%disasterType);
	
	if(%disasterType)
		messageAll('MsgAdminForce',"\c6Warning! Readings show that the next disaster will be \c0abnormal\c6!");
	
    %disaster = getRandom(0, %disastersCount);
	
	while(DisastersList.getObject(%disaster).type != %disasterType) { // ineefficient but nvm
		%disaster = getRandom(0, %disastersCount);
	}
	
    $Disasters::LastDisaster = $Disasters::CurrentDisaster;
    $Disasters::CurrentDisaster = DisastersList.getObject(%disaster);
    
    while($Disasters::LastDisaster == $Disasters::CurrentDisaster) {
        // NO
        // BAD MOD
        // BAD
        
        %disaster = getRandom(0, %disastersCount);
        $Disasters::CurrentDisaster = DisastersList.getObject(%disaster);
    }
    
    echo("Selected disaster" SPC %disaster @ ":" SPC $Disasters::CurrentDisaster.name);
	
	$Disasters::TimeLimit = $Disasters::CurrentDisaster.roundTime;
}

function startCurrentDisaster() {
	$DefaultMinigame.fallingDamage = true;
	$DefaultMiniGame.weaponDamage = true;
	
	$DefaultMiniGame.setEnableBuilding(false);
	
	$Disasters::Running = true;
	$Disasters::Start = getSimTime();

	messageAll('',"\c6Oh no!\c0" SPC $Disasters::CurrentDisaster.name @ "\c6! Be the \c0LAST MAN STANDING \c6or survive for " @ getTimeString($Disasters::TimeLimit) @ "!");
	
    schedule(2000, 0, call, $Disasters::CurrentDisaster.startFunction);
}

function stopCurrentDisaster() {
	$DefaultMinigame.fallingDamage = false;
	$DefaultMiniGame.weaponDamage = false;
	
	%mg = $DefaultMiniGame;
	
	for(%i=0;%i<%mg.numMembers;%i++)
	{
		%client = %mg.member[%i];

		%client.player.setShapeNameColor("1.0 1.0 1.0");
		
		// take tools
		%client.player.clearTools();
		
		// give building tools
		%client.giveTools();
	}
	
	$DefaultMiniGame.setEnableBuilding(true);
	
	$Disasters::Running = false;
	
    cancel($Disasters::CurrentDisasterLoop);
    
    %disObj = $Disasters::CurrentDisaster;
    
    if(%disObj.hasStopFunction) {
        call(%disObj.stopFunction);
    }
}

function cameraOnly(%client)
{
	if(isObject(%client.player))
		%client.player.delete();

	%camera = %client.camera;
	%camera.setFlyMode();
	%camera.mode = "Observer";
	%client.setControlObject(%camera);
}

function serverCmdCheckPlayers(%client)
{
	%mg = $DefaultMinigame;
	for(%i=0;%i<%mg.numMembers;%i++)
	{
		%target = %mg.member[%i];
		if(%target.player)
			%alive = "\c2[ALIVE]";
		else
			%alive = "\c0[DEAD]";

		messageClient(%client,'',"\c0 " SPC %target.name SPC %alive);
	}
}

function serverCmdCheckDisasters(%client)
{
	for(%i = 0; %i < DisastersList.getCount(); %i++) {
		%disaster = DisastersList.getObject(%i);
		messageClient(%client,'',"\c6" @ %i @ (%disaster.type == 0 ? "\c1" : "\c0") SPC %disaster.name);
	}
}

function serverCmdPeekDisaster(%client) {
	if(!%client.isAdmin)
		return;
	
	messageClient(%client,'',"\c0 " SPC $Disasters::CurrentDisaster.name);
}

function serverCmdSeeRecord(%client) {
	messageClient(%client,'',"\c6The current record is \c0" @ $Disasters::Record SPC "\c6disasters survived in a row before death, held by\c0" SPC $Disasters::RecordName @ "\c6.");
}

function serverCmdForceDisaster(%client, %n) {
	if(!%client.isAdmin || $Disasters::Running)
		return;
	
	$Disasters::LastDisaster = $Disasters::CurrentDisaster;
    $Disasters::CurrentDisaster = DisastersList.getObject(%n);
	
	$Disasters::TimeLimit = $Disasters::CurrentDisaster.roundTime;
	
	messageClient(%client,'',"\c6Forcing\c0" SPC $Disasters::CurrentDisaster.name);
	commandToClient(%client, 'DNextInfo', $Disasters::CurrentDisaster.name);
	
	messageAll('MsgAdminForce',"\c0" @ %client.name SPC "\c6is forcing a disaster.");
}

function serverCmdUpgrade(%cl, %upgrade) {
    messageClient(%cl,'',"\c6I'll code it one day!");
}

function newDisasterCycle() {
	cancel($startDisasterSched);
	$Disasters::Round++;

	pickRandomDisaster();
	
	messageAll('',"\c6In " @ getTimeString($Disasters::Prep) @ ", a\c0 DISASTER\c6 will happen! Prepare well - you don't know what could appear.");
	$startDisasterSched = schedule($Disasters::Prep * 1000, 0, startCurrentDisaster);
	
	$DefaultMiniGame.setEnableBuilding(true);
}

function GameConnection::giveTools(%client) {
	%client.player.addNewItem("Hammer");
	
	if($Disasters::Wrench $= "1") {
		%client.player.addNewItem("Limited Wrench");
	}
	else if($Disasters::Wrench $= "2") {
		%client.player.addNewItem(wrenchItem);
	}
	
	%client.player.addNewItem("Printer");
}

package DisastersPackage
{
	function GameConnection::onClientEnterGame(%c)
	{       
		Parent::onClientEnterGame(%c);
		
		if(!$Disasters::Running)
			%c.schedule(2000, spawnPlayer);
	}
	
	function GameConnection::spawnPlayer(%c)
	{       
		Parent::spawnPlayer(%c);
		
		if(!$Disasters::Running) {
			%c.schedule(1000, giveTools);
		}
	}
	
	function GameModeInitialResetCheck()
	{
		parent::GameModeInitialResetCheck();

		if($Disasters::HasLoaded)
			return;

		$Disasters::HasLoaded = 1;
		$Disasters::Running = false;
	}
	
	function MiniGameSO::Reset(%obj,%client)
	{
		cancel($Disasters::SwapSchedule);
		cancel($DisastersTick);
        cancel($startDisasterSched);
        
		$Disasters::HasWon = 0;
		$Disasters::HasWarned = 0;
		$Disasters::Round = 0;
        
		%mg = $DefaultMinigame;
        
        stopCurrentDisaster();

		for(%i=0;%i<%mg.numMembers;%i++)
		{
			%mg.member[%i].player.setDatablock(PlayerNoJet);
		}
		
		messageAll('', "\c6It's a new day. \c0Everyone has been revived\c6.");
		parent::Reset(%obj,%client);
		
		// start a disaster
		newDisasterCycle();
		
		tickDisasters();
		
		for(%i=0;%i<%mg.numMembers;%i++)
		{
			%client = %mg.member[%i];

			%client.player.setShapeNameColor("1.0 1.0 1.0");
		}
		
		$DefaultMiniGame.setEnableBuilding(true);
	}

	function onServerDestroyed()
	{
		$Disasters::HasLoaded = 0;
		cancel($DisastersTick);
		parent::onServerDestroyed();
	}

	function tickDisasters()
	{
		cancel($DisastersTick);
		%mg = $DefaultMinigame;
		
		if(!$Disasters::Running) {
			commandToAll('bottomPrint',"\c6" @ getTimeString(mFloor(getTimeRemaining($startDisasterSched) / 1000)), 2);
		}
		else if(getSimTime() - $Disasters::Start > $Disasters::TimeLimit * 1000 && !$Disasters::HasWon)
		{
			%count = 0;
			%revived = 0;
			
			for(%i=0;%i<$DefaultMinigame.numMembers;%i++)
			{
				%client = $DefaultMinigame.member[%i];
				
				if(!isObject(%client.player)) {
					if(%revived < 4) {
						$DefaultMinigame.member[%i].spawnPlayer();
						%client.survivals = 0;
						//%revived++;
					}
				}
				else {
					%client.survivals++;
					%client.setScore(%client.survivals);
					%count++;
				}
			}
			
			$Disasters::Running = false;
			stopCurrentDisaster();
			
			messageAll('',"\c0" @ %count SPC "\c6people survived this round. In addition, dead players have been revived.");
			
			schedule(5000, 0, newDisasterCycle());
		}
		else if(getSimTime() - $Disasters::Start > ($Disasters::TimeLimit/2) * 1000 && !$Disasters::HasWon && !$Disasters::HasWarned)
		{
			messageAll('',"\c6" @ getTimeString($Disasters::TimeLimit/2) SPC "remaining");
			$Disasters::HasWarned = 1;
		}

		$DisastersTick = schedule(1000,0,tickDisasters);
	}
    
	function GameConnection::onDeath(%client,%obj,%killer,%type,%area)
	{
		parent::onDeath(%client,%obj,%killer,%type,%area);
		
		%mg = $DefaultMinigame;

		%count = 0;
		
		if(!$Disasters::Running) {
			%client.schedule(1000, spawnPlayer);
			return;
		}
		
		for(%i=0;%i<%mg.numMembers;%i++)
		{
			if(!%mg.member[%i].isEnemy && %mg.member[%i].player)
				%count++;
		}
		
		if(%count > 0)
			commandToAll('bottomPrint',"\c3" @ %count-1 SPC "\c5players remain<just:right>\c3" @ getTimeString((getSimTime() - $Disasters::Start)/1000) SPC "\c5elapsed",2);
	
		if(%client.survivals > $Disasters::Record) {
			$Disasters::Record = %client.survivals;
			$Disasters::RecordName = %client.name;
			messageAll('',"\c0" @ %client.name SPC "\c6has beaten the current record for disasters survived in a row. The record is now\c0" SPC $Disasters::Record SPC "\c6.");
		}
		
		%client.setScore(0);
	}
	
	function FxDTSBrick::onFakeDeath(%brick,%proj)
	{
		parent::onFakeDeath(%brick,%proj);
		
		if($Disasters::Destruction $= "1")
			%brick.schedule(200, killBrick);
	}

	function SimObject::onCameraEnterOrbit(%target,%camera)
	{
		parent::onCameraEnterOrbit(%target,%camera);

		%target = %target.client;
		%client = %camera.getControllingClient();
		
		if(%target.isEnemy)
			%client.bottomPrint("\c6Observing\c0" SPC %target.name,4);
		else
			%client.bottomPrint("\c6Observing\c2" SPC %target.name,4);
	} 
    
    function Armor::onTrigger(%datablock,%player,%slot,%val)
	{
		Parent::onTrigger(%datablock,%player,%slot,%val);
		if(%slot == 3) //Here we check if a player is crouching and return a value if he is.
			%player.isCrouching = %val; //Here we modify the value depending on if they are crouching. 1 for yes, 0 for no.
	}
	
	function miniGameCanDamage(%obj1,%obj2)
	{
      if($Disasters::Running)
         return 1; // if a disaster's happening everything hurts
      else
         return parent::miniGameCanDamage(%obj1,%obj2);
	}
	
	// DOORS/COFFINS ETC FIX
	function fxDTSBrick::onLoadPlant( %obj )
	{
		parent::onLoadPlant( %obj );
		
		if(isObject(%obj.client)) {
			%obj.onPlant();
		}
	}
	
	function fxDTSBrick::onPlant( %obj ) {
		if(isObject(%obj.client)) {
			%obj.client.wrenchLimited = false;
		}
		
		parent::onPlant( %obj );
	}
};
activatePackage(DisastersPackage);

createDisasterList();

// disable baseplates
brick16x32fData.uiName = "";
brick32x32fData.uiName = "";
brick48x48fData.uiName = "";
brick64x64fData.uiName = "";

// disable roads
brick32x32froadxData.uiName = "";
brick32x32froadcData.uiName = "";
brick32x32froadtData.uiName = "";
brick32x32froadsData.uiName = "";

// disable hammer damage
hammerProjectile.directDamage = 0;

// disable tank (but keep its projectiles)
tankVehicle.uiName = "";
tankTurretPlayer.uiName = "";