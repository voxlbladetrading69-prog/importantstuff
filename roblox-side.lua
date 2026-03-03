task.wait(5)

local Players = game:GetService("Players")
local RunService = game:GetService("RunService")

local player = Players.LocalPlayer
local Character = player.Character or player.CharacterAdded:Wait()

local INTERVAL = 30 -- seconds
local FREEZE_FACTOR = 2
local FILE_NAME = "REJOINER.txt"

local running = true
local lastStep = os.clock()

-- ===== MESSAGE HELPER =====

local function showMessage(text)
	task.spawn(function()
		local msg = Instance.new("Message")
		msg.Text = tostring(text)
		msg.Parent = workspace

		task.wait(2)

		if msg then
			msg:Destroy()
		end
	end)
end

-- ===== HEARTBEAT WRITER =====

showMessage("[HB] Local heartbeat writer started for " .. player.Name)

local function safeWriteHeartbeat()
	local ts = os.time()
	local ok, err = pcall(function()
		writefile(FILE_NAME, tostring(ts))
	end)

	if not ok then
		showMessage("[HB] Failed to write heartbeat")
		return false
	end

	return true
end

-- ===== stopping signals =====

player.Kicked:Connect(function(reason)
	showMessage("[HB] Kicked: " .. tostring(reason))
	running = false
end)

Players.PlayerRemoving:Connect(function(plr)
	if plr == player then
		showMessage("[HB] Player removing")
		running = false
	end
end)

RunService.Heartbeat:Connect(function()
	lastStep = os.clock()
end)

-- initial write
safeWriteHeartbeat()

-- ===== main loop =====

while running do
	task.wait(INTERVAL)

	if not RunService:IsRunning() then
		showMessage("[HB] Engine stopped")
		break
	end

	if os.clock() - lastStep > INTERVAL * FREEZE_FACTOR then
		showMessage("[HB] Client frozen detected")
		break
	end

	safeWriteHeartbeat()
end

player:Kick("HAS STOPPED RUNNING ALREADY")
showMessage("[HB] Heartbeat stopped for " .. player.Name)
