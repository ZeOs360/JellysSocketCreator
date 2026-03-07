// ============================================================
// JellysSockets - Custom CPU Socket Mod for PC Building Simulator 2
// Author: JellysSockets Team
// Version: 1.0.0
// ============================================================

#include <windows.h>
#include <MinHook.h>
#include <fstream>
#include <sstream>
#include <map>
#include <string>
#include <vector>
#pragma comment(lib, "C:\\Users\\ZeOs\\Downloads\\PCBS2\\MinHook_134_lib\\lib\\libMinHook.x64.lib")

// ============================================================
// PROXY - version.dll forwarding
// ============================================================
#pragma comment(linker, "/export:GetFileVersionInfoA=C:\\Windows\\System32\\version.GetFileVersionInfoA")
#pragma comment(linker, "/export:GetFileVersionInfoSizeA=C:\\Windows\\System32\\version.GetFileVersionInfoSizeA")
#pragma comment(linker, "/export:GetFileVersionInfoSizeW=C:\\Windows\\System32\\version.GetFileVersionInfoSizeW")
#pragma comment(linker, "/export:GetFileVersionInfoW=C:\\Windows\\System32\\version.GetFileVersionInfoW")
#pragma comment(linker, "/export:VerQueryValueA=C:\\Windows\\System32\\version.VerQueryValueA")
#pragma comment(linker, "/export:VerQueryValueW=C:\\Windows\\System32\\version.VerQueryValueW")

// ============================================================
// CUSTOM SOCKET DEFINITIONS - Loaded from config file
// ============================================================
std::map<int, std::string> CustomSocketNames;

// ============================================================
// IL2CPP API TYPES
// ============================================================
typedef void* (*il2cpp_string_new_t)(const char* str);
typedef void* (*il2cpp_array_new_t)(void* klass, uintptr_t count);
typedef uintptr_t (*il2cpp_array_length_t)(void* array);
typedef void* (*il2cpp_domain_get_t)();
typedef void** (*il2cpp_domain_get_assemblies_t)(void* domain, size_t* size);
typedef void* (*il2cpp_assembly_get_image_t)(void* assembly);
typedef void* (*il2cpp_class_from_name_t)(void* image, const char* ns, const char* name);
typedef void* (*il2cpp_class_get_field_from_name_t)(void* klass, const char* name);
typedef void  (*il2cpp_field_static_get_value_t)(void* field, void* value);
typedef void  (*il2cpp_field_static_set_value_t)(void* field, void* value);
typedef const char* (*il2cpp_image_get_name_t)(void* image);
typedef int   (*il2cpp_field_get_offset_t)(void* field);
typedef void* (*il2cpp_class_get_method_from_name_t)(void* klass, const char* name, int argsCount);
typedef void* (*il2cpp_thread_attach_t)(void* domain);

il2cpp_thread_attach_t            il2cpp_thread_attach = nullptr;
il2cpp_string_new_t              il2cpp_string_new = nullptr;
il2cpp_array_new_t               il2cpp_array_new = nullptr;
il2cpp_array_length_t            il2cpp_array_length = nullptr;
il2cpp_domain_get_t              il2cpp_domain_get = nullptr;
il2cpp_domain_get_assemblies_t   il2cpp_domain_get_assemblies = nullptr;
il2cpp_assembly_get_image_t      il2cpp_assembly_get_image = nullptr;
il2cpp_class_from_name_t         il2cpp_class_from_name = nullptr;
il2cpp_class_get_field_from_name_t il2cpp_class_get_field_from_name = nullptr;
il2cpp_field_static_get_value_t  il2cpp_field_static_get_value = nullptr;
il2cpp_field_static_set_value_t  il2cpp_field_static_set_value = nullptr;
il2cpp_image_get_name_t          il2cpp_image_get_name = nullptr;
il2cpp_field_get_offset_t        il2cpp_field_get_offset = nullptr;
il2cpp_class_get_method_from_name_t il2cpp_class_get_method_from_name = nullptr;

// Field offsets
int g_cpuSocketFieldOffset = -1;
int g_mbSocketFieldOffset  = -1;

// Patch success flags
static bool g_namesPatched = false;
static bool g_usedPatched  = false;

// Log directory (game folder)
static std::string g_logDir;

// ============================================================
// CONFIG FILE PARSER - Simple JSON-like format
// File: JellysSockets.json
// Format: {"sockets":[{"id":100,"name":"AM6"},{"id":101,"name":"AM7"}]}
// ============================================================
bool LoadConfig(const std::string& configPath, std::ofstream& log) {
    std::ifstream file(configPath);
    if (!file.is_open()) {
        log << "[!] Config file not found: " << configPath << std::endl;
        log << "[!] Create JellysSockets.json with your custom sockets." << std::endl;
        return false;
    }

    std::stringstream buffer;
    buffer << file.rdbuf();
    std::string content = buffer.str();
    file.close();

    // Simple parsing - find "id": and "name": pairs
    size_t pos = 0;
    while ((pos = content.find("\"id\"", pos)) != std::string::npos) {
        // Find the ID number
        size_t colonPos = content.find(':', pos);
        if (colonPos == std::string::npos) break;
        
        size_t numStart = content.find_first_of("0123456789", colonPos);
        if (numStart == std::string::npos) break;
        
        size_t numEnd = content.find_first_not_of("0123456789", numStart);
        if (numEnd == std::string::npos) numEnd = content.length();
        
        int socketId = std::stoi(content.substr(numStart, numEnd - numStart));
        
        // Find the name
        size_t namePos = content.find("\"name\"", numEnd);
        if (namePos == std::string::npos || namePos > content.find('}', numEnd)) {
            pos = numEnd;
            continue;
        }
        
        size_t nameColonPos = content.find(':', namePos);
        if (nameColonPos == std::string::npos) break;
        
        size_t nameQuoteStart = content.find('"', nameColonPos + 1);
        if (nameQuoteStart == std::string::npos) break;
        
        size_t nameQuoteEnd = content.find('"', nameQuoteStart + 1);
        if (nameQuoteEnd == std::string::npos) break;
        
        std::string socketName = content.substr(nameQuoteStart + 1, nameQuoteEnd - nameQuoteStart - 1);
        
        // Validate socket ID (must be >= 100 to avoid conflicts)
        if (socketId >= 100 && socketId < 1000) {
            CustomSocketNames[socketId] = socketName;
            log << "[+] Loaded socket: ID=" << socketId << " Name=\"" << socketName << "\"" << std::endl;
        } else {
            log << "[!] Invalid socket ID " << socketId << " (must be 100-999)" << std::endl;
        }
        
        pos = nameQuoteEnd;
    }

    log << "[+] Total custom sockets loaded: " << CustomSocketNames.size() << std::endl;
    return !CustomSocketNames.empty();
}

// ============================================================
// IL2CppString helper functions
// IL2CppString: [klass*8][monitor*8][length int32][chars wchar_t[]]
// ============================================================
static bool Il2CppStrEquals(void* il2str, const wchar_t* expected) {
    if (!il2str) return false;
    int len = *(int32_t*)((uintptr_t)il2str + 0x10);
    int expLen = (int)wcslen(expected);
    if (len != expLen) return false;
    return memcmp((void*)((uintptr_t)il2str + 0x14), expected, len * 2) == 0;
}

static std::string Il2CppStrToStd(void* il2str) {
    if (!il2str) return "";
    int len = *(int32_t*)((uintptr_t)il2str + 0x10);
    wchar_t* chars = (wchar_t*)((uintptr_t)il2str + 0x14);
    std::string r;
    for (int i = 0; i < len; i++) r += (char)chars[i];
    return r;
}

// ============================================================
// ImportProp HOOK - Handles XML parsing for CPU Socket field
// Native signature: bool(void* this, void* name, void* value, void* errorStack, void* methodInfo)
// ============================================================
typedef bool (*ImportProp_t)(void* thisPtr, void* nameStr, void* valueStr, void* errorStack, void* methodInfo);
ImportProp_t Original_ImportProp_CPU = nullptr;
ImportProp_t Original_ImportProp_MB  = nullptr;

static bool HandleSocketImport(void* thisPtr, void* nameStr, void* valueStr, int fieldOffset) {
    if (fieldOffset < 0) return false;
    if (!Il2CppStrEquals(nameStr, L"CPU Socket")) return false;

    std::string val = Il2CppStrToStd(valueStr);

    // Match by custom socket name
    for (const auto& p : CustomSocketNames) {
        if (val == p.second) {
            *(int*)((uintptr_t)thisPtr + fieldOffset) = p.first;
            return true;
        }
    }
    
    // Try parsing as integer string (e.g., "100", "101")
    try {
        int id = std::stoi(val);
        if (CustomSocketNames.count(id)) {
            *(int*)((uintptr_t)thisPtr + fieldOffset) = id;
            return true;
        }
    } catch (...) {}

    return false;
}

bool Hooked_ImportProp_CPU(void* thisPtr, void* nameStr, void* valueStr, void* errorStack, void* methodInfo) {
    if (HandleSocketImport(thisPtr, nameStr, valueStr, g_cpuSocketFieldOffset))
        return true;
    return Original_ImportProp_CPU(thisPtr, nameStr, valueStr, errorStack, methodInfo);
}

bool Hooked_ImportProp_MB(void* thisPtr, void* nameStr, void* valueStr, void* errorStack, void* methodInfo) {
    if (HandleSocketImport(thisPtr, nameStr, valueStr, g_mbSocketFieldOffset))
        return true;
    return Original_ImportProp_MB(thisPtr, nameStr, valueStr, errorStack, methodInfo);
}

// ============================================================
// IsCompatible HOOK - Makes custom sockets compatible with each other
// Native: bool(int socketA, int socketB, MethodInfo*)
// ============================================================
typedef bool (*IsCompatible_t)(int socketA, int socketB, void* methodInfo);
IsCompatible_t Original_IsCompatible = nullptr;

bool Hooked_IsCompatible(int socketA, int socketB, void* methodInfo) {
    // If both are the same custom socket, they're compatible
    if (socketA >= 100 && socketB >= 100 && socketA == socketB)
        return true;
    return Original_IsCompatible(socketA, socketB, methodInfo);
}

// ============================================================
// GetUIName HOOK - Returns display name for custom sockets
// Native: Il2CppString*(int cpuSocket, MethodInfo*)
// ============================================================
typedef void* (*GetUIName_t)(int cpuSocket, void* methodInfo);
GetUIName_t Original_GetUIName = nullptr;

void* Hooked_GetUIName(int cpuSocket, void* methodInfo) {
    if (cpuSocket >= 100 && CustomSocketNames.count(cpuSocket)) {
        return il2cpp_string_new(CustomSocketNames[cpuSocket].c_str());
    }
    return Original_GetUIName(cpuSocket, methodInfo);
}

// ============================================================
// s_names PATCH - Extends CpuSocketExt.s_names array
// ============================================================
void* FindAssemblyCSharpImage() {
    void* domain = il2cpp_domain_get();
    if (!domain) return nullptr;
    size_t asmCount = 0;
    void** assemblies = il2cpp_domain_get_assemblies(domain, &asmCount);
    if (!assemblies) return nullptr;
    for (size_t i = 0; i < asmCount; i++) {
        if (!assemblies[i]) continue;
        void* img = il2cpp_assembly_get_image(assemblies[i]);
        if (!img) continue;
        const char* nm = il2cpp_image_get_name(img);
        if (nm && strcmp(nm, "Assembly-CSharp.dll") == 0) return img;
    }
    return nullptr;
}

bool PatchSocketNamesField(std::ofstream& log) {
    void* targetImage = FindAssemblyCSharpImage();
    if (!targetImage) return false;

    void* cpuSocketExtClass = il2cpp_class_from_name(targetImage, "", "CpuSocketExt");
    if (!cpuSocketExtClass) { log << "[-] CpuSocketExt class not found" << std::endl; return false; }

    void* namesField = il2cpp_class_get_field_from_name(cpuSocketExtClass, "s_names");
    if (!namesField) { log << "[-] s_names field not found" << std::endl; return false; }

    void* curArr = nullptr;
    il2cpp_field_static_get_value(namesField, &curArr);
    if (!curArr) { log << "[-] s_names is null" << std::endl; return false; }

    uintptr_t oldLen = il2cpp_array_length(curArr);
    log << "[+] Current s_names length: " << std::dec << oldLen << std::endl;

    // Find max socket ID we need
    int maxId = 0;
    for (const auto& p : CustomSocketNames) {
        if (p.first > maxId) maxId = p.first;
    }
    
    uintptr_t newLen = (uintptr_t)(maxId + 1);
    if (newLen <= oldLen) {
        log << "[+] s_names already large enough" << std::endl;
        newLen = oldLen;
    }

    // Get Il2CppString class for array creation
    void* stringClass = il2cpp_class_from_name(targetImage, "System", "String");
    if (!stringClass) {
        void* domain = il2cpp_domain_get();
        size_t asmCount = 0;
        void** assemblies = il2cpp_domain_get_assemblies(domain, &asmCount);
        for (size_t i = 0; i < asmCount && !stringClass; i++) {
            if (!assemblies[i]) continue;
            void* img = il2cpp_assembly_get_image(assemblies[i]);
            if (!img) continue;
            stringClass = il2cpp_class_from_name(img, "System", "String");
        }
    }
    if (!stringClass) { log << "[-] String class not found" << std::endl; return false; }

    void* newArr = il2cpp_array_new(stringClass, newLen);
    if (!newArr) { log << "[-] Failed to create new array" << std::endl; return false; }

    // Copy existing strings
    const uintptr_t HDR = 0x20;
    const uintptr_t ELEM_SIZE = 8;
    memcpy((void*)((uintptr_t)newArr + HDR), (void*)((uintptr_t)curArr + HDR), oldLen * ELEM_SIZE);

    // Initialize new slots with empty strings
    void* emptyStr = il2cpp_string_new("");
    for (uintptr_t i = oldLen; i < newLen; i++) {
        *(void**)((uintptr_t)newArr + HDR + i * ELEM_SIZE) = emptyStr;
    }

    // Set custom socket names
    for (const auto& p : CustomSocketNames) {
        void* nameStr = il2cpp_string_new(p.second.c_str());
        *(void**)((uintptr_t)newArr + HDR + p.first * ELEM_SIZE) = nameStr;
        log << "[+] s_names[" << p.first << "] = \"" << p.second << "\"" << std::endl;
    }

    il2cpp_field_static_set_value(namesField, newArr);

    // Verify
    void* verify = nullptr;
    il2cpp_field_static_get_value(namesField, &verify);
    return (verify == newArr);
}

// ============================================================
// s_used PATCH - Marks custom sockets as "in use"
// ============================================================
bool PatchSocketUsed(std::ofstream& log) {
    void* targetImage = FindAssemblyCSharpImage();
    if (!targetImage) return false;

    void* cpuSocketExtClass = il2cpp_class_from_name(targetImage, "", "CpuSocketExt");
    if (!cpuSocketExtClass) return false;

    void* usedField = il2cpp_class_get_field_from_name(cpuSocketExtClass, "s_used");
    if (!usedField) { log << "[-] s_used field not found" << std::endl; return false; }

    void* curArr = nullptr;
    il2cpp_field_static_get_value(usedField, &curArr);
    if (!curArr) { log << "[-] s_used is null" << std::endl; return false; }

    uintptr_t oldLen = il2cpp_array_length(curArr);
    log << "[+] Current s_used length: " << std::dec << oldLen << std::endl;

    int maxId = 0;
    for (const auto& p : CustomSocketNames) {
        if (p.first > maxId) maxId = p.first;
    }
    
    uintptr_t newLen = (uintptr_t)(maxId + 1);
    if (newLen <= oldLen) newLen = oldLen;

    void* boolClass = il2cpp_class_from_name(targetImage, "System", "Boolean");
    if (!boolClass) {
        void* domain = il2cpp_domain_get();
        size_t asmCount = 0;
        void** assemblies = il2cpp_domain_get_assemblies(domain, &asmCount);
        for (size_t i = 0; i < asmCount && !boolClass; i++) {
            if (!assemblies[i]) continue;
            void* img = il2cpp_assembly_get_image(assemblies[i]);
            if (!img) continue;
            boolClass = il2cpp_class_from_name(img, "System", "Boolean");
        }
    }
    if (!boolClass) { log << "[-] Boolean class not found" << std::endl; return false; }

    void* newArr = il2cpp_array_new(boolClass, newLen);
    if (!newArr) { log << "[-] Failed to create s_used array" << std::endl; return false; }

    const uintptr_t HDR = 0x20;
    const uintptr_t ELEM_SIZE = 1;
    memcpy((void*)((uintptr_t)newArr + HDR), (void*)((uintptr_t)curArr + HDR), oldLen * ELEM_SIZE);

    // Mark custom sockets as used
    for (const auto& p : CustomSocketNames) {
        *(uint8_t*)((uintptr_t)newArr + HDR + p.first * ELEM_SIZE) = 1;
        log << "[+] s_used[" << p.first << "] = true (" << p.second << ")" << std::endl;
    }

    il2cpp_field_static_set_value(usedField, newArr);

    void* verify = nullptr;
    il2cpp_field_static_get_value(usedField, &verify);
    return (verify == newArr);
}

// ============================================================
// Get native method pointer from MethodInfo
// Il2CppMethodInfo layout: [methodPointer void*] at offset 0
// ============================================================
void* GetNativeMethodPointer(void* methodInfo) {
    if (!methodInfo) return nullptr;
    return *(void**)methodInfo;
}

// ============================================================
// MAIN THREAD
// ============================================================
DWORD WINAPI MainThread(LPVOID lpParam) {
    // Wait for GameAssembly.dll
    HMODULE hGA = nullptr;
    while ((hGA = GetModuleHandleA("GameAssembly.dll")) == nullptr) Sleep(100);

    // Get game directory for logs and config
    char gaPath[MAX_PATH] = {};
    GetModuleFileNameA(hGA, gaPath, MAX_PATH);
    g_logDir = std::string(gaPath);
    g_logDir = g_logDir.substr(0, g_logDir.find_last_of("\\/") + 1);

    // Clean old logs
    DeleteFileA((g_logDir + "JellysSockets.log").c_str());

    std::ofstream log(g_logDir + "JellysSockets.log");
    log << "========================================" << std::endl;
    log << "JellysSockets v1.0.0 - Custom CPU Sockets" << std::endl;
    log << "========================================" << std::endl;
    log << "[+] DLL injected successfully" << std::endl;
    log << "[+] Game directory: " << g_logDir << std::endl;
    log.flush();

    // Load config file
    std::string configPath = g_logDir + "JellysSockets.json";
    if (!LoadConfig(configPath, log)) {
        log << "[!] No custom sockets configured." << std::endl;
        log << "[!] Create JellysSockets.json with format:" << std::endl;
        log << "[!] {\"sockets\":[{\"id\":100,\"name\":\"MySocket\"}]}" << std::endl;
        log.close();
        return 0;
    }
    log.flush();

    // Load IL2CPP functions
    auto L = [&](const char* n) -> void* {
        void* f = GetProcAddress(hGA, n);
        if (!f) log << "[-] " << n << " not found" << std::endl;
        return f;
    };

    il2cpp_string_new                = (il2cpp_string_new_t)             L("il2cpp_string_new");
    il2cpp_array_new                 = (il2cpp_array_new_t)              L("il2cpp_array_new");
    il2cpp_array_length              = (il2cpp_array_length_t)           L("il2cpp_array_length");
    il2cpp_domain_get                = (il2cpp_domain_get_t)             L("il2cpp_domain_get");
    il2cpp_domain_get_assemblies     = (il2cpp_domain_get_assemblies_t)  L("il2cpp_domain_get_assemblies");
    il2cpp_assembly_get_image        = (il2cpp_assembly_get_image_t)     L("il2cpp_assembly_get_image");
    il2cpp_class_from_name           = (il2cpp_class_from_name_t)        L("il2cpp_class_from_name");
    il2cpp_class_get_field_from_name = (il2cpp_class_get_field_from_name_t) L("il2cpp_class_get_field_from_name");
    il2cpp_field_static_get_value    = (il2cpp_field_static_get_value_t) L("il2cpp_field_static_get_value");
    il2cpp_field_static_set_value    = (il2cpp_field_static_set_value_t) L("il2cpp_field_static_set_value");
    il2cpp_image_get_name            = (il2cpp_image_get_name_t)         L("il2cpp_image_get_name");
    il2cpp_field_get_offset          = (il2cpp_field_get_offset_t)       L("il2cpp_field_get_offset");
    il2cpp_class_get_method_from_name = (il2cpp_class_get_method_from_name_t) L("il2cpp_class_get_method_from_name");
    il2cpp_thread_attach = (il2cpp_thread_attach_t) L("il2cpp_thread_attach");

    bool critOk = il2cpp_string_new && il2cpp_array_new && il2cpp_array_length &&
                  il2cpp_domain_get && il2cpp_domain_get_assemblies && il2cpp_assembly_get_image &&
                  il2cpp_class_from_name && il2cpp_class_get_field_from_name &&
                  il2cpp_field_static_get_value && il2cpp_field_static_set_value && il2cpp_image_get_name &&
                  il2cpp_class_get_method_from_name;

    if (!critOk) { log << "[-] Some IL2CPP functions missing!" << std::endl; log.close(); return 0; }
    log << "[+] IL2CPP API ready" << std::endl;
    log.flush();

    // Attach thread to IL2CPP runtime
    void* domain = il2cpp_domain_get();
    if (domain && il2cpp_thread_attach) {
        il2cpp_thread_attach(domain);
        log << "[+] Thread attached to IL2CPP" << std::endl;
    }
    log.flush();

    // Wait for Assembly-CSharp
    void* asmImage = nullptr;
    for (int i = 0; i < 60 && !asmImage; i++) {
        asmImage = FindAssemblyCSharpImage();
        if (!asmImage) Sleep(500);
    }
    if (!asmImage) { log << "[-] Assembly-CSharp not found!" << std::endl; log.close(); return 0; }
    log << "[+] Assembly-CSharp loaded" << std::endl;
    log.flush();

    // Find classes
    void* cpuClass = il2cpp_class_from_name(asmImage, "", "PartDescCPU");
    void* mbClass  = il2cpp_class_from_name(asmImage, "", "PartDescMotherboard");

    // Get m_socket field offsets
    if (cpuClass && il2cpp_field_get_offset) {
        void* f = il2cpp_class_get_field_from_name(cpuClass, "m_socket");
        if (f) g_cpuSocketFieldOffset = il2cpp_field_get_offset(f);
    }
    if (mbClass && il2cpp_field_get_offset) {
        void* f = il2cpp_class_get_field_from_name(mbClass, "m_socket");
        if (f) g_mbSocketFieldOffset = il2cpp_field_get_offset(f);
    }
    log << "[+] CPU m_socket offset: " << g_cpuSocketFieldOffset << std::endl;
    log << "[+] MB m_socket offset: " << g_mbSocketFieldOffset << std::endl;
    log.flush();

    // Get ImportProp native addresses
    void* cpuImportPropNative = nullptr;
    void* mbImportPropNative  = nullptr;

    if (cpuClass) {
        void* mi = il2cpp_class_get_method_from_name(cpuClass, "ImportProp", 3);
        if (mi) cpuImportPropNative = GetNativeMethodPointer(mi);
    }
    if (mbClass) {
        void* mi = il2cpp_class_get_method_from_name(mbClass, "ImportProp", 3);
        if (mi) mbImportPropNative = GetNativeMethodPointer(mi);
    }

    // Initialize MinHook
    if (MH_Initialize() != MH_OK) { log << "[-] MinHook init failed!" << std::endl; log.close(); return 0; }
    log << "[+] MinHook initialized" << std::endl;

    // Create hooks
    if (cpuImportPropNative && g_cpuSocketFieldOffset >= 0) {
        MH_STATUS s = MH_CreateHook(cpuImportPropNative, &Hooked_ImportProp_CPU, (LPVOID*)&Original_ImportProp_CPU);
        log << "[" << (s == MH_OK ? "+" : "-") << "] CPU ImportProp hook: " << (s == MH_OK ? "OK" : "FAILED") << std::endl;
    }

    if (mbImportPropNative && g_mbSocketFieldOffset >= 0) {
        MH_STATUS s = MH_CreateHook(mbImportPropNative, &Hooked_ImportProp_MB, (LPVOID*)&Original_ImportProp_MB);
        log << "[" << (s == MH_OK ? "+" : "-") << "] MB ImportProp hook: " << (s == MH_OK ? "OK" : "FAILED") << std::endl;
    }

    // IsCompatible hook (hardcoded RVA)
    uintptr_t base = (uintptr_t)hGA;
    uintptr_t isCompAddr = base + 0x211E38;
    {
        MH_STATUS s = MH_CreateHook((LPVOID)isCompAddr, &Hooked_IsCompatible, (LPVOID*)&Original_IsCompatible);
        log << "[" << (s == MH_OK ? "+" : "-") << "] IsCompatible hook: " << (s == MH_OK ? "OK" : "FAILED") << std::endl;
    }

    // GetUIName hook
    {
        void* cpuSocketExtClass = il2cpp_class_from_name(asmImage, "", "CpuSocketExt");
        if (cpuSocketExtClass) {
            void* uiNameMI = il2cpp_class_get_method_from_name(cpuSocketExtClass, "GetUIName", 1);
            if (uiNameMI) {
                void* nativeUIName = GetNativeMethodPointer(uiNameMI);
                MH_STATUS s = MH_CreateHook(nativeUIName, &Hooked_GetUIName, (LPVOID*)&Original_GetUIName);
                log << "[" << (s == MH_OK ? "+" : "-") << "] GetUIName hook: " << (s == MH_OK ? "OK" : "FAILED") << std::endl;
            }
        }
    }

    // Enable all hooks
    if (MH_EnableHook(MH_ALL_HOOKS) == MH_OK)
        log << "[+] All hooks enabled" << std::endl;
    else
        log << "[-] Hook activation failed!" << std::endl;

    log << "[+] Custom sockets registered: ";
    for (const auto& p : CustomSocketNames) log << p.first << "=" << p.second << " ";
    log << std::endl;
    log.flush();

    // Patch loop - wait for game to initialize
    log << "[+] Waiting for game initialization..." << std::endl;
    log.flush();

    for (int i = 0; i < 30; i++) {
        Sleep(2000);

        if (!g_namesPatched) g_namesPatched = PatchSocketNamesField(log);
        if (!g_usedPatched)  g_usedPatched  = PatchSocketUsed(log);

        if (g_namesPatched && g_usedPatched) {
            log << "[+] All patches applied successfully!" << std::endl;
            break;
        }
        log.flush();
    }

    log << "========================================" << std::endl;
    log << "JellysSockets initialization complete" << std::endl;
    log << "========================================" << std::endl;
    log.close();
    return 0;
}

// ============================================================
// DLL ENTRY POINT
// ============================================================
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
    if (ul_reason_for_call == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(hModule);
        CreateThread(nullptr, 0, MainThread, nullptr, 0, nullptr);
    }
    return TRUE;
}
