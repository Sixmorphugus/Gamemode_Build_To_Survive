datablock itemData(lWrenchItem : wrenchItem)
{
	category = "Weapon";
	uiName = "Limited Wrench";
	image = lWrenchImage;
	colorShiftColor = lWrenchImage.colorShiftColor;
};

datablock shapeBaseImageData(lWrenchImage : wrenchImage)
{
	// SpaceCasts
	raycastWeaponRange = 8;
	raycastWeaponTargets = $Typemasks::FxBrickAlwaysObjectType;
	raycastDirectDamage = 0;
	raycastExplosionProjectile = wrenchProjectile;
	
	item = lWrenchItem;
	projectile = wrenchProjectile;
	colorShiftColor = "0.400000 0.750000 0.750000 1.000000";
	showBricks = 0;
	
	// State change needed so that the wrench will autofire without reclicking.
	stateTransitionOnTriggerUp[4] = "";
	stateTransitionOnTriggerDown[4] = "StopFire";
};

function lWrenchImage::onPreFire(%this, %obj, %slot)
{
	// Ain't got no fucking idea what the thread name is, so this works.
	wrenchImage.onPreFire(%obj, %slot);
}

function lWrenchImage::onStopFire(%this, %obj, %slot)
{
	%obj.playThread(2, "root");
}

function lWrenchImage::onFire(%this, %obj, %slot)
{
	if(isObject(%obj.client)) {
		%obj.client.wrenchLimited = true;
		%obj.client.wrenchWarned = false;
	}
	
	if(%this.raycastWeaponRange > 0)
	{
		// Directional Math (Pythagoras)
		%fvec		= %obj.getForwardVector();
		%fX			= getWord(%fvec,0);
		%fY			= getWord(%fvec,1);
		
		%evec		= %obj.getEyeVector();
		%eX			= getWord(%evec,0);
		%eY			= getWord(%evec,1);
		%eZ			= getWord(%evec,2);
		
		%eXY		= mSqrt((%eX * %eX) + (%eY * %eY));
		%aimVec		= (%fX * %eXY) SPC (%fY * %eXY) SPC %eZ;
		
		// Range
		%range		= %this.raycastWeaponRange * getWord(%obj.getScale(), 2);
		
		// 100 is the maximum effective range of a raycast.
		if(%range > 100)
		{
			%rangeRem = %range - 100;
			%range = 100;
		}
		
		// Raycast Parameters
		%start		= %obj.getEyePoint();
		%end[0]		= vectorAdd(%start, vectorScale(%aimVec, %range)); // [0] required for loop
		%targets	= %this.raycastWeaponTargets;
		
		// Raycasting
		%ray		= ContainerRayCast(%start, %end[0], %targets, %obj);
		%col		= getWord(%ray, 0);
		
		if(isObject(%col))
		{
			%pos	= posFromRaycast(%ray);
			%normal	= normalFromRaycast(%ray);
			%end[0]	= %pos;
			%colclass	= %col.getClassName();
			
			return %this.onHitObject(%obj, %slot, %col, %pos, %normal, %objsHit);
		}
		
		if(%rangeRem > 0)
		{
			for(%a = 1; %a <= 10 && %rangeRem > 0; %a++)
			{
				%range		= %rangeRem;
				
				if(%range > 100)
				{
					%rangeRem = %range - 100;
					%range = 100;
				}
				
				%start[%a]	= %end[%a - 1];
				%end[%a]	= vectorAdd(%start[%a], vectorScale(%aimVec, %range)); 
				
				%ray		= ContainerRayCast(%start[%a], %end[%a], %targets, %obj);
				%col		= getWord(%ray, 0);
				
				if(isObject(%col))
				{
					%pos		= posFromRaycast(%ray);
					%normal		= normalFromRaycast(%ray);
					%end[%a]	= %pos;
					%colclass	= %col.getClassName();
					
					return %this.onHitObject(%obj, %slot, %col, %pos, %normal, %objsHit);
				}
			}
		}
	}
	else
	{
		return Parent::onFire(%this, %obj, %slot);
	}
}

function lWrenchImage::onHitObject(%this, %obj, %slot, %col, %pos, %normal)
{
	if(isObject(%client = %obj.client) && (%col.getType() & $Typemasks::FxBrickAlwaysObjectType))
	{
		%grp = getBrickgroupFromObject(%col);
		
		%display = %grp.name;
		
		if(getTrustLevel(%client,%col) >= 2) {
			ServerPlay3d(wrenchHitSound, %pos);
			if(%client.hasLWDialog && %col.dataBlock !$= "brickVehicleSpawnData" && %col.dataBlock !$= "brickMusicData") {
				// show our gui
				commandToClient(%client, 'showLWrenchDlg', %display);
				lWrench_sendBrickInfo(%client, %col);
			}
			else {
				// show default gui
				WrenchImage::onHitObject(%this, %obj, %slot, %col, %pos, %normal);
			}
		} else {
			ServerPlay3d(wrenchMissSound, %pos);
			commandToClient(%client, 'centerPrint', %display SPC "does not trust you enough to do that.", 3);
		}
	}
	else {
		ServerPlay3d(wrenchMissSound, %pos);
	}

	if(isObject(%this.raycastExplosionProjectile))
	{
		%scaleFactor = getWord(%obj.getScale(), 2);
		
		%p = new Projectile()
		{
			dataBlock		= %this.raycastExplosionProjectile;
			initialPosition	= %pos;
			initialVelocity	= %normal;
			sourceObject	= %obj;
			client			= %obj.client;
			sourceSlot		= 0;
			originPoint		= %pos;
		};
		MissionCleanup.add(%p);
		
		%p.setScale(%scaleFactor SPC %scaleFactor SPC %scaleFactor);
		%p.explode();
	}
}

// data (oh GOD)
function serverCmdEnableLWrench(%client) {
	%client.hasLWDialog = true;
}

function serverCmdDisableLWrench(%client) {
	%client.hasLWDialog = false;
}

function serverCmdSendLWrenchData(%client) {
	// this function is called when a client first wrenches a brick on the server.
	// it'll send them all the datablocks that are available to them.
	
	for(%i=0;%i<DatablockGroup.getCount();%i++)
	{
		%obj = DatablockGroup.getObject(%i);
		if(%obj.getClassName() $= "fxLightData")
			commandToClient(%client, 'LWrenchLight', %obj.uiName, %obj);
		if(%obj.getClassName() $= "ParticleEmitterData")
			commandToClient(%client, 'LWrenchEmitter', %obj.uiName, %obj);
	}
}

function serverCmdLWrenchNewName(%client, %var) {
	%brick = %client.wrenchBrick;
	
	%brick.setLight(%var);
}

function serverCmdLWrenchNewLight(%client, %var) {
	%brick = %client.wrenchBrick;
	
	%brick.setLight(%var);
}

function serverCmdLWrenchNewEmitter(%client, %var) {
	%brick = %client.wrenchBrick;
	
	%brick.setEmitter(%var);
}

function serverCmdLWrenchNewName(%client, %name) {
	%brick = %client.wrenchBrick;
	
	if(%name $= "")
    {
		// someone told me to do this?
		// yeah
		%brick.setname("");
	}
	else
	{
		%brick.setNTObjectName(%name);
	}
}

function serverCmdLWrenchNewEmitterDir(%client, %var) {
	%brick = %client.wrenchBrick;
	
	%brick.setEmitterDirection(%var);
}

function serverCmdLWrenchNewRendering(%client, %var) {
	%brick = %client.wrenchBrick;
	
	%brick.setRendering(%var);
}

function lWrench_sendBrickInfo(%client, %obj) {
	%client.wrenchBrick = %obj; // make sure to unset this so people can't do hax
	
	if(isObject(%obj.getLightId())) {
		commandToClient(%client, 'LWrenchCurLight', %obj.getLightId().getDatablock());
	}
	else {
		commandToClient(%client, 'LWrenchCurLight', -1);
	}
	
	// send emitter
	if(isObject(%obj.emitter)) {
		commandToClient(%client, 'LWrenchCurEmitter', nameToId(%obj.emitter.emitter));
	}
	else {
		commandToClient(%client, 'LWrenchCurEmitter', -1);
	}
	
	commandToClient(%client, 'LWrenchCurEmitterDir', %obj.emitterDirection);
	commandToClient(%client, 'LWrenchCurRendering', %obj.isRendering());
	
	if(strLen(%obj.getName()) > 0)
		commandToClient(%client, 'LWrenchCurname', getSubStr(%obj.getName(),1,strLen(%obj.getName())-1));
	else
		commandToClient(%client, 'LWrenchCurname', "");
}

if(isPackage(lWrenchServer))
	deactivatePackage(lWrenchServer);

package lWrenchServer {
	function GameConnection::autoAdminCheck(%client)
	{
		Parent::autoAdminCheck(%client);
		commandToClient(%client, 'lWrenchCheck');
	}
	
	function wrenchImage::onFire(%this, %obj, %slot) {
		if(isObject(%obj.client))
			%obj.client.wrenchLimited = false;
		
		Parent::onFire(%this, %obj, %slot);
	}
	
	function serverCmdSetWrenchData(%client, %data) {
		%player = %client.player;
		
		if(!isObject(%player)) {
			return; //rip
		}
		
		// check the player is actually holding a wrench
		if(%client.wrenchLimited) {
			// remove any disallowed fields
			
			// item
			%count = getFieldCount(%data);

			for (%i = 0; %i < %count; %i++)
			{
				%field = getField(%data, %i);

				// modifications attempted
				%warn = false;
				
				if (firstWord(%field) $= "IDB") {
					%entry = getWord(%field, 1);
					%data = setField(%data, %i, "IDB 0");
					
					if(%entry !$= "0") {
						%warn = true;
					}
				}
				else if (firstWord(%field) $= "RC") {
					%entry = getWord(%field, 1);
					%data = setField(%data, %i, "RC 1");
					
					if(%entry !$= "1") {
						%warn = true;
					}
				}
				else if (firstWord(%field) $= "C") {
					%entry = getWord(%field, 1);
					%data = setField(%data, %i, "C 1");
					
					if(%entry !$= "1") {
						%warn = true;
					}
				}
				
				if(!%client.wrenchWarned && %warn) {
					%client.wrenchWarned = true;
					MessageClient(%client, '', "Some of the settings you tried to modify cannot be changed with a limited wrench.");
					
					if(!%client.hasLWDialog)
						MessageClient(%client, '', "For a more concise menu, please get the client.");
				}
			}
		}
		
		// done
		Parent::serverCmdSetWrenchData(%client, %data);
	}
	
	function serverCmdAddEvent(%client, %delay, %input, %target, %a, %b, %output, %par1, %par2, %par3, %par4) {
		if(%client.wrenchLimited) {
			if(!%client.wrenchWarned) {
				%client.wrenchWarned = true;
				MessageClient(%client, '', "You cannot add events with a limited wrench.");
				
				if(!%client.hasLWDialog)
					MessageClient(%client, '', "For a more concise menu, please get the client.");
			}
			
			return; // no events allowed with limited wrench
		}
		
		Parent::serverCmdAddEvent(%client, %delay, %input, %target, %a, %b, %output, %par1, %par2, %par3, %par4);
	}
};

activatePackage(lWrenchServer);