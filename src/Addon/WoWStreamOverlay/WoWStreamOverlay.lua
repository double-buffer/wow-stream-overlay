local frame = CreateFrame("Frame")

local ADDON_HANDLE = "WoWStreamOverlay"
local DEBUG_CALLBACK_HANDLE = "WoWStreamOverlayDebug"

local enabledByUs = false
local activeLoggingLibrary = nil
local debugCallbacksRegistered = false

local function Debug(message)
    DEFAULT_CHAT_FRAME:AddMessage("|cff00bfff[WoWStreamOverlay]|r " .. message)
end

local function GetCombatLoggingLibrary()
    if not LibStub or type(LibStub.GetLibrary) ~= "function" then
        return nil
    end

    return LibStub:GetLibrary("LibCombatLogging-1.0", true)
end

local function GetLoggingStateDescription()
    local library = GetCombatLoggingLibrary()
    if not library then
        return "lib=none"
    end

    local loggers = library.GetLoggingAddOns() or "none"
    local loggerCount = library.GetNumLogging()
    local ourHandle = library.IsLogging(ADDON_HANDLE)

    return string.format(
        "lib=available | handles=%d | ours=%s | loggers=%s",
        loggerCount,
        tostring(ourHandle),
        loggers)
end

local function TraceEvent(event)
    local instanceName, instanceType, difficultyID, _, _, _, _, instanceMapID = GetInstanceInfo()

    Debug(string.format(
        "%s | zone=%s | type=%s | difficulty=%s | map=%s | %s",
        event,
        tostring(instanceName),
        tostring(instanceType),
        tostring(difficultyID),
        tostring(instanceMapID),
        GetLoggingStateDescription()))
end

local function RegisterLibraryDebugCallbacks()
    if debugCallbacksRegistered then
        return
    end

    local library = GetCombatLoggingLibrary()
    if not library or not library.CallbackEvents then
        return
    end

    library.RegisterCallback(
        DEBUG_CALLBACK_HANDLE,
        library.CallbackEvents.ADDON_STARTED_LOGGING,
        function(_, addon)
            Debug("LibCombatLogging START: " .. tostring(addon) .. " | " .. GetLoggingStateDescription())
        end)

    library.RegisterCallback(
        DEBUG_CALLBACK_HANDLE,
        library.CallbackEvents.ADDON_STOPPED_LOGGING,
        function(_, addon)
            Debug("LibCombatLogging STOP: " .. tostring(addon) .. " | " .. GetLoggingStateDescription())
        end)

    debugCallbacksRegistered = true
end

local function StopCombatLogging()
    if not enabledByUs then
        return
    end

    local result

    if activeLoggingLibrary then
        result = activeLoggingLibrary.LoggingCombat(ADDON_HANDLE, false)
    else
        result = LoggingCombat(false)
    end

    if result == nil then
        Debug("bootstrap stop was rate-limited; retrying in 10 seconds")
        C_Timer.After(10.0, StopCombatLogging)
        return
    end

    enabledByUs = false
    activeLoggingLibrary = nil
end

local function OnPlayerEnteringWorld(isInitialLogin)
    if not isInitialLogin then
        return
    end

    local _, instanceType = GetInstanceInfo()
    if instanceType == "party" or instanceType == "raid" then
        Debug("bootstrap skipped because login occurred inside an instance")
        return
    end

    -- This is the only diagnostic-path read of WoW's native LoggingCombat API.
    -- If somebody is already logging, leave the physical state untouched.
    local isLogging = LoggingCombat()

    if isLogging == nil then
        Debug("bootstrap skipped because native logging state is temporarily unavailable")
        return
    end

    if isLogging then
        Debug("bootstrap skipped because combat logging is already active")
        return
    end

    local library = GetCombatLoggingLibrary()
    local result

    if library then
        result = library.LoggingCombat(ADDON_HANDLE, true)
    else
        result = LoggingCombat(true)
    end

    if result ~= true then
        Debug("bootstrap could not enable combat logging")
        return
    end

    enabledByUs = true
    activeLoggingLibrary = library

    Debug("bootstrap enabled combat logging via " .. (library and "LibCombatLogging" or "native API"))
    C_Timer.After(1.0, StopCombatLogging)
end

local tracedEvents = {
    "PLAYER_ENTERING_WORLD",
    "LOADING_SCREEN_DISABLED",
    "ZONE_CHANGED",
    "ZONE_CHANGED_NEW_AREA",
    "ZONE_CHANGED_INDOORS",
    "CHALLENGE_MODE_START",
}

for _, event in ipairs(tracedEvents) do
    frame:RegisterEvent(event)
end

frame:SetScript("OnEvent", function(_, event, ...)
    RegisterLibraryDebugCallbacks()
    TraceEvent(event)

    if event == "PLAYER_ENTERING_WORLD" then
        OnPlayerEnteringWorld(...)
    end
end)