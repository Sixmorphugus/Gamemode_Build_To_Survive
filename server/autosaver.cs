$Pref::Server::RTBAS_Enabled = true;
$Pref::Server::RTBAS_AnnounceSave = true;
$Pref::Server::RTBAS_SaveInterval = 10;
$Pref::Server::RTBAS_SaveOwnership = 2;

function servercmdAutosaveNow(%client)
{
	if(!%client.isAdmin || !isObject(RTB_AutoSaver))
		return;

	RTB_AutoSaver._doAutoSave();
}

//***************************************************
//* Initialization
//***************************************************
function RTB_AutoSaver::initialise()
{
	if(isObject(RTB_AutoSaver))
		return;

	new ScriptObject(RTB_AutoSaver){};

	RTB_AutoSaver.startAutoSave();
}
RTB_AutoSaver::initialise();

//***************************************************
//* Auto Save
//***************************************************
function RTB_AutoSaver::startAutoSave(%this)
{
	cancel(%this.autoSave);

	if(!$Pref::Server::RTBAS_Enabled)
		return;

	%this.autoSave = %this.schedule(($Pref::Server::RTBAS_SaveInterval * 60) @ "000", "_doAutoSave");
}

function RTB_AutoSaver::_doAutoSave(%this)
{
	cancel(%this.autoSave);

	%filename = "RTBAS/" @ strReplace(getWord(getDateTime(), 0),"/","-") @ "_" @ strReplace(getWord(getDateTime(),1),":","-") @ ".bls";
	%this.saveBricks(%filename);

	%this.autoSave = %this.schedule(($Pref::Server::RTBAS_SaveInterval * 60) @ "000", "_doAutoSave");
}

//***************************************************
//* Save Bricks
//***************************************************
function RTB_AutoSaver::saveBricks(%this, %filename)
{
	if(%this.saving)
		return;

	%bricks = getBrickCount(); 
	if(%bricks <= 0)
		return;

	if($Pref::Server::RTBAS_AnnounceSave)
		messageAll('', 'Saving bricks. Please wait.');
   
	%this.saveBricks = 0;
	%this.saveStart = $Sim::Time;
	%this.saveFilename = %filename;
	%this.saving = true;

	%this.savePath = "saves/" @ %filename;
	  
	%file = %this.saveFile = new FileObject();
	%file.openForWrite(%this.savePath);
	%file.writeLine("This is a Blockland save file. You probably shouldn't modify it cause you'll screw it up.");
	%file.writeLine("1");
	%file.writeLine("RTB Hosting Automated Save File - Taken at " @ getDateTime());
   
	for(%i=0;%i<64;%i++)
		%file.writeLine(getColorIDTable(%i));
   
	%file.writeLine("Linecount TBD");

	%this._brickGroupDig(0, ($Pref::Server::RTBAS_SaveOwnership == 1));
}

//- RTB_AutoSaver::_saveEnd (called when saving is complete)
function RTB_AutoSaver::_saveEnd(%this)
{
	%this.saveFile.close();
	%this.saveFile.delete();

	%this.saving = false;
   
	%diff = mFloatLength($Sim::Time - %this.saveStart,2);
	if(getSubStr(%diff,0,1) $= "-")
		%diff = "0.00";
   
	if($Pref::Server::RTBAS_AnnounceSave)
		messageAll('', '%1 bricks saved in %2', %this.saveBricks, getTimeString(%diff));
}

//- RTB_AutoSaver::_writeBrick (writes data for a single brick to file)
function RTB_AutoSaver::_writeBrick(%this, %brick)
{
	RTB_AutoSaver.saveBricks++;
   
	%print = (%brick.getDataBlock().hasPrint) ? getPrintTexture(%brick.getPrintID()) : "";
   
	%file = %this.saveFile;
	%file.writeLine(%brick.getDataBlock().uiName @ "\" " @ %brick.getPosition() SPC %brick.getAngleID() SPC %brick.isBasePlate() SPC %brick.getColorID() SPC %print SPC %brick.getColorFXID() SPC %brick.getShapeFXID() SPC %brick.isRayCasting() SPC %brick.isColliding() SPC %brick.isRendering());
   
	if((%brick.isBasePlate() && $Pref::Server::RTBAS_SaveOwnership == 1) || $Pref::Server::RTBAS_SaveOwnership == 2)
		%file.writeLine("+-OWNER " @ getBrickGroupFromObject(%brick).bl_id);
	if(%brick.getName() !$= "")
		%file.writeLine("+-NTOBJECTNAME " @ %brick.getName());
	if(isObject(%brick.emitter))
		%file.writeLine("+-EMITTER " @ %brick.emitter.emitter.uiName @ "\" " @ %brick.emitterDirection);
	if(%brick.getLightID() >= 0)
		%file.writeLine("+-LIGHT " @ %brick.getLightID().getDataBlock().uiName @ "\" ");
	if(isObject(%brick.item))
		%file.writeLine("+-ITEM " @ %brick.item.getDataBlock().uiName @ "\" " @ %brick.itemPosition SPC %brick.itemDirection SPC %brick.itemRespawnTime);
	if(isObject(%brick.audioEmitter))
		%file.writeLine("+-AUDIOEMITTER " @ %brick.audioEmitter.getProfileID().uiName @ "\" ");
	if(isObject(%brick.vehicleSpawnMarker))
		%file.writeLine("+-VEHICLE " @  %brick.vehicleSpawnMarker.uiName @ "\" " @ %brick.reColorVehicle);
	  
	for(%i=0;%i<%brick.numEvents;%i++)
	{
		%targetClass = %brick.eventTargetIdx[%i] >= 0 ? getWord(getField($InputEvent_TargetListfxDTSBrick_[%brick.eventInputIdx[%i]], %brick.eventTargetIdx[%i]), 1) : "fxDtsBrick";
		%paramList = $OutputEvent_parameterList[%targetClass, %brick.eventOutputIdx[%i]];
		%params = "";
		for(%j=0;%j<getFieldCount(%paramList);%j++)
		{
			if(firstWord(getField(%paramList, %j)) $= "dataBlock" && %brick.eventOutputParameter[%i, %j+1] >= 0)
				%params = %params TAB %brick.eventOutputParameter[%i, %j+1].getName();
			else
				%params = %params TAB %brick.eventOutputParameter[%i, %j+1];
		}
		%file.writeLine("+-EVENT" TAB %i TAB %brick.eventEnabled[%i] TAB %brick.eventInput[%i] TAB %brick.eventDelay[%i] TAB %brick.eventTarget[%i] TAB %brick.eventNT[%i] TAB %brick.eventOutput[%i] @ %params);
	}
}

//- RTB_AutoSaver::_brickGroupDig (digs through brick groups)
function RTB_AutoSaver::_brickGroupDig(%this, %index, %baseplates)
{
	if(%index >= mainBrickGroup.getCount())
	{
		if(%baseplates == 1)
			%this.schedule(1,"_brickGroupDig", 0, 2);
		else
			%this._saveEnd();

		return;
	}
	%this._brickDig(mainBrickGroup.getObject(%index),mainBrickGroup.getObject(%index).getCount()-1, %index, %baseplates);
}

//- RTB_AutoSaver::_brickDig (recurses through a brick group in a semi-non-blocking way)
function RTB_AutoSaver::_brickDig(%this, %group, %index, %mainIndex, %baseplates)
{
	if(%index >= %group.getCount())
		%index = %group.getCount() - 1;

	if(%index < 0)
	{
		%this.schedule(1,"_brickGroupDig", %mainIndex++, %baseplates);
		return;
	}

	for(%i=%index;%i>%index-100 && %i >= 0;%i--)
	{
		%brick = %group.getObject(%i);

		if(%baseplates == 0
		|| %baseplates == 1 && %brick.isBasePlate()
		|| %baseplates == 2 && !%brick.isBasePlate())
			%this._writeBrick(%group.getObject(%i));
	}

	%this.schedule(1,"_brickDig", %group, %i, %mainIndex, %baseplates);
}