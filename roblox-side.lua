task.wait(5)
local Players = game:GetService("Players")
local RunService = game:GetService("RunService")
local Character = Player.Character or Player.CharacterAdded:Wait()

local player = Players.LocalPlayer

local INTERVAL = 30 -- seconds
local FREEZE_FACTOR = 2
local FILE_NAME = "REJOINER.txt"

local running = true
local lastStep = os.clock()

print("[HB] Local heartbeat writer started for", player.Name)

local function safeWriteHeartbeat()
	local ts = os.time()
	local ok, err = pcall(function()
		writefile(FILE_NAME, tostring(ts))
	end)

	if not ok then
		warn("[HB] Failed to write heartbeat:", err)
		return false
	end

	return true
end

-- ===== stopping signals =====

player.Kicked:Connect(function(reason)
	print("[HB] Kicked:", reason)
	running = false
end)

Players.PlayerRemoving:Connect(function(plr)
	if plr == player then
		print("[HB] Player removing")
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
		print("[HB] Engine stopped")
		break
	end

	if os.clock() - lastStep > INTERVAL * FREEZE_FACTOR then
		print("[HB] Client frozen detected")
		break
	end

	safeWriteHeartbeat()
end

player:Kick("HAS STOPPED RUNNING ALREADY")
print("[HB] Heartbeat stopped for", player.Name)

