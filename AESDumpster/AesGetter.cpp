#include "./OtherTools/OtherTools.h"
#include "./KeyTools/KeyDumpster.h"

extern "C" __declspec(dllexport) wchar_t* GetAesKey(const wchar_t* exe_path)
{
    OtherTools* other_tools = new OtherTools();

    if (other_tools->CreateExeBuffer(exe_path) != 0)
    {
        delete other_tools;
        return nullptr;
    }

    if (other_tools->retval.buffer == nullptr)
    {
        delete other_tools;
        return nullptr;
    }

    KeyDumpster* key_dumpster = new KeyDumpster();
    if (!key_dumpster->FindAESKeys(other_tools->retval.buffer, other_tools->retval.size))
    {
        delete other_tools;
        delete key_dumpster;

        return nullptr;
    }
    else
    {
        std::vector<std::string> keys = key_dumpster->GetKeyInformation();
        std::wstringstream wss;

        for (const auto& key : keys)
        {
            wss << std::wstring(key.begin(), key.end()) << L'\n';
        }

        delete other_tools;
        delete key_dumpster;

        std::wstring result = wss.str();
        wchar_t* result_cstr = new wchar_t[result.size() + 1];
        wcscpy_s(result_cstr, result.size() + 1, result.c_str());

        return result_cstr;
    }
}
