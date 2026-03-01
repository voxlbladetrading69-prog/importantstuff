local Players = game:GetService("Players")
local player = Players.LocalPlayer

local INTERVAL = 30 -- seconds
local FILE_NAME = "REJOINER.txt"

print("[HB] Local heartbeat writer started for", player.Name)

-- write initial heartbeat immediately
local function writeHeartbeat()
	local ts = os.time()
	writefile(FILE_NAME, tostring(ts))
	-- optional debug
	-- print("[HB] Wrote timestamp:", ts)
end

-- first write (important so Termux doesn't see "never")
writeHeartbeat()

-- main loop
while player.Parent ~= nil do
	task.wait(INTERVAL)
	writeHeartbeat()
end

print("[HB] Client shutting down:", player.Name)
