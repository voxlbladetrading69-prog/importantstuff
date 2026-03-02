local Players = game:GetService("Players")
local RunService = game:GetService("RunService")

local player = Players.LocalPlayer

local INTERVAL = 30 -- seconds
local FILE_NAME = "REJOINER.txt"

local running = true
local lastStep = os.clock()

print("[HB] Local heartbeat writer started for", player.Name)

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

-- detect client freeze / engine stall
RunService.Heartbeat:Connect(function()
	lastStep = os.clock()
end)

-- ===== heartbeat writer =====

local function writeHeartbeat()
	local ts = os.time()
	writefile(FILE_NAME, tostring(ts))
end

-- initial write
writeHeartbeat()

-- ===== main loop =====

while running do
	task.wait(INTERVAL)

	-- engine no longer running
	if not RunService:IsRunning() then
		print("[HB] Engine stopped")
		break
	end

	-- client frozen (no Heartbeat for too long)
	if os.clock() - lastStep > INTERVAL * 2 then
		print("[HB] Client frozen detected")
		break
	end

	writeHeartbeat()
end

print("[HB] Heartbeat stopped for", player.Name)
