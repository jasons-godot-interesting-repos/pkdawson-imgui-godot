#pragma warning(push, 0)
#include <godot_cpp/classes/display_server.hpp>
#include <godot_cpp/classes/engine.hpp>
#include <godot_cpp/classes/main_loop.hpp>
#include <godot_cpp/classes/scene_tree.hpp>
#include <godot_cpp/core/class_db.hpp>
#include <godot_cpp/core/defs.hpp>
#include <godot_cpp/godot.hpp>
#include <godot_cpp/variant/utility_functions.hpp>
#pragma warning(pop)

#include "ImGuiAPI.h"
#include "ImGuiGD.h"
#include "ImGuiGodotHelper.h"
#include "ImGuiLayer.h"
#include "ImGuiRoot.h"

using namespace godot;

void initialize_ign_module(ModuleInitializationLevel p_level)
{
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE)
        return;

    ClassDB::register_class<ImGui::Godot::ImGui>();
    ClassDB::register_class<ImGui::Godot::ImGuiRoot>();
    ClassDB::register_class<ImGui::Godot::ImGuiLayer>();
    ClassDB::register_class<ImGui::Godot::ImGuiGodotHelper>();
    ClassDB::register_class<ImGui::Godot::ImGuiGD>();
}

void uninitialize_ign_module(ModuleInitializationLevel p_level)
{
    if (p_level != MODULE_INITIALIZATION_LEVEL_SCENE)
        return;
}

extern "C" {
GDExtensionBool GDE_EXPORT ign_init(GDExtensionInterfaceGetProcAddress p_get_proc_address,
                                    GDExtensionClassLibraryPtr p_library, GDExtensionInitialization* r_initialization)
{
    GDExtensionBinding::InitObject init_obj(p_get_proc_address, p_library, r_initialization);

    init_obj.register_initializer(initialize_ign_module);
    init_obj.register_terminator(uninitialize_ign_module);
    init_obj.set_minimum_library_initialization_level(MODULE_INITIALIZATION_LEVEL_SCENE);

    return init_obj.init();
}
}
