#pragma once
#include "Renderer.h"
#include <imgui.h>
#include <memory>

#pragma warning(push, 0)
#include <godot_cpp/variant/rid.hpp>
#pragma warning(pop)

using godot::RID;

namespace ImGui::Godot {

class CanvasRenderer : public Renderer
{
public:
    CanvasRenderer();
    virtual ~CanvasRenderer();

    virtual const char* Name() override
    {
        return "godot4_canvas";
    }

    bool Init() override;
    void InitViewport(RID vprid) override;
    void CloseViewport(RID vprid) override;
    void Render() override;
    void OnHide() override;

private:
    struct Impl;
    std::unique_ptr<Impl> impl;
};

} // namespace ImGui::Godot
