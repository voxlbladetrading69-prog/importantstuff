local HttpService = game:GetService("HttpService")

local proxyUrl = "https://unpaid-slave.voxlblade-trading69.workers.dev/webhooks/1477552950942629889/2-7Dp7c_GJmKnQKdWeqNw0IGOU5l7CtOvlDKgRRQnjZ67NOwADW80WEDL11NNWRh4Q2Y"

local payload //help here please, implement the HWID, package stuff and yeah
local success, response = pcall(function()
	return HttpService:RequestAsync({
		Url = proxyUrl,
		Method = "POST",
		Headers = {
			["Content-Type"] = "application/json"
		},
		Body = HttpService:JSONEncode(payload)
	})
end)

if success then
	print("Webhook sent:", response.StatusCode)
else
	warn("Failed to send webhook:", response)
end
