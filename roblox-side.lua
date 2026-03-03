task.wait(5)

local Players = game:GetService("Players")
local RunService = game:GetService("RunService")
local StarterGui = game:GetService("StarterGui")
local GuiService = game:GetService("GuiService")

local player = Players.LocalPlayer
local Character = player.Character or player.CharacterAdded:Wait()

local INTERVAL = 15 -- seconds
local FREEZE_FACTOR = 2
local FILE_NAME = "REJOINER.txt"

local running = true
local lastStep = os.clock()

-- ===== NOTIFICATION HELPER =====

local function notify(text)
	task.spawn(function()
		pcall(function()
			StarterGui:SetCore("SendNotification", {
				Title = "Heartbeat",
				Text = tostring(text),
				Duration = 10
			})
		end)
	end)
end

-- ===== HEARTBEAT WRITER =====

notify("Local heartbeat writer started for " .. player.Name)

local function safeWriteHeartbeat()
	local ts = os.time()
	local ok, err = pcall(function()
		writefile(FILE_NAME, tostring(ts))
	end)
	
	notify("WRITING heartbeat")
	
	if not ok then
		notify("Failed to write heartbeat")
		return false
	end

	return true
end

-- ===== stopping signals =====

Players.PlayerRemoving:Connect(function(plr)
	if plr == player then
		notify("Player removing")
		running = false
	end
end)

RunService.Heartbeat:Connect(function()
	lastStep = os.clock()
end)

-- ===== CONNECTION FAILURE DETECTION =====

pcall(function()
	local NetworkClient = game:GetService("NetworkClient")

	NetworkClient.ConnectionFailed:Connect(function(_, reason)
		notify("Connection failed")
		running = false
	end)
end)

local function onErrorMessageChanged(errorMessage)
    if true then --if errorMessage and errorMessage ~= "" then
        notify("Connection failed")
        if player then
            running = false
        end
    end
end
GuiService.ErrorMessageChanged:Connect(onErrorMessageChanged)

-- initial write
safeWriteHeartbeat()

-- ===== main loop =====

while running do
	task.wait(INTERVAL)

	if not RunService:IsRunning() then
		notify("Engine stopped")
		break
	end

	if os.clock() - lastStep > INTERVAL * FREEZE_FACTOR then
		notify("Client freeze detected")
		break
	end

	safeWriteHeartbeat()
end

player:Kick("HAS STOPPED RUNNING ALREADY")
notify("Heartbeat stopped for " .. player.Name)

