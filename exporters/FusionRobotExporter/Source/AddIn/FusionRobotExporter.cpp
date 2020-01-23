#include <Core/CoreAll.h>
#include <Fusion/FusionAll.h>
#include "EUI.h"
#include "Identifiers.h"

// does this not have a header file for linking?

using namespace adsk::core;

Ptr<Application> app;
Ptr<UserInterface> UI;

SynthesisAddIn::EUI * EUI = nullptr;


void unroll_exception(const std::exception& e, int level = 0)
{
	//std::cerr << std::string(level, ' ') << "exception: " << e.what() << '\n';
	if (UI) {
		UI->messageBox(level + " -> " + std::string(e.what()));
		std::string errorMessage;

		//try to get the fusion related error maybe?
		int errorCode = app->getLastError(&errorMessage);
		if (GenericErrors::Ok != errorCode)
			UI->messageBox(errorMessage);

		if (SynthesisAddIn::Analytics::IsEnabled()) {
			// doesn't allow const char* so let's see if this cast even works.
			SynthesisAddIn::Analytics::LogEvent(U("Error"), utility::conversions::to_string_t(std::string((e.what()))));
		}
	}

	// This will only really work if the nested exception handling uses std::throw_with_nested as I suggest it should.
	try {
		std::rethrow_if_nested(e);
	}
	catch (const std::exception & e) {
		unroll_exception(e, level + 1); // use std::throw_with_nested( (exception type) ) and use a catch(...) on all nested exceptions plz
	}
	catch (...) {}
}

extern "C" XI_EXPORT bool run(const char* context)
{
	app = Application::get();
	if (!app)
		return false;

	UI = app->userInterface();
	if (!UI)
		return false;

	// I guess we actually need the UI to make a message box.. huh
	try {
		EUI = new SynthesisAddIn::EUI(UI, app);

		return true;
	}
	catch (const std::exception & e) {
		// Hacky way to fix some error reporting
		UI->messageBox("Exception has occured please report to shawn.hice@autodesk.com : \n" + std::string(e.what()));
		unroll_exception(e);
		return false;
	}
}

extern "C" XI_EXPORT bool stop(const char* context)
{
	if (UI)
	{
		delete EUI;
		EUI = nullptr;

		// Delete reference to UI
		app = nullptr;
		UI = nullptr;
	}

	return true;
}

#ifdef XI_WIN

#include <windows.h>

BOOL APIENTRY DllMain(HMODULE hmodule, DWORD reason, LPVOID reserved)
{
	switch (reason)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

#endif // XI_WIN
