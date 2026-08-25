local frame = CreateFrame("Frame")

local enabledByUs = false

local function StopCombatLogging()
    if not enabledByUs then
        return
    end

    LoggingCombat(false)
    enabledByUs = false
end

local function OnPlayerEnteringWorld(isInitialLogin)
    if not isInitialLogin then
        return
    end

    local isLogging = LoggingCombat()

    if isLogging == nil or isLogging then
        return
    end

    if LoggingCombat(true) ~= true then
        return
    end

    enabledByUs = true

    C_Timer.After(1.0, StopCombatLogging)
end

frame:RegisterEvent("PLAYER_ENTERING_WORLD")

frame:SetScript("OnEvent", function(self, event, ...)
    if event == "PLAYER_ENTERING_WORLD" then
        OnPlayerEnteringWorld(...)
    end
end)