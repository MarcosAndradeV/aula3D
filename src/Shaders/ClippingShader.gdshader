shader_type spatial;
render_mode cull_disabled; // Renderiza frente e verso (backfaces)

// --- PLANOS DE CORTE (CLIPPING PLANES) globais ---
global uniform vec3 plane_normal_x = vec3(1.0, 0.0, 0.0);
global uniform float plane_distance_x = 1000.0; // Valor alto para não cortar por padrão

global uniform vec3 plane_normal_y = vec3(0.0, 1.0, 0.0);
global uniform float plane_distance_y = 1000.0;

global uniform vec3 plane_normal_z = vec3(0.0, 0.0, 1.0);
global uniform float plane_distance_z = 1000.0;

// --- PROPRIEDADES DO MATERIAL BASE (injetadas via C#) ---
uniform vec4 albedo_color : source_color = vec4(1.0);
uniform sampler2D texture_albedo : source_color, filter_linear_mipmap, repeat_enable;
uniform float roughness : hint_range(0,1) = 0.5;
uniform float metallic : hint_range(0,1) = 0.0;
uniform vec4 emission_color : source_color = vec4(0.0);

void fragment() {
    // 1. Encontra a posição global deste pixel
    vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
    
    // 2. Lógica de Discard (Corte):
    // Se a posição do pixel for MENOR que a distância na direção da normal do plano, joga fora.
    if (dot(world_pos, plane_normal_x) < plane_distance_x ||
        dot(world_pos, plane_normal_y) < plane_distance_y ||
        dot(world_pos, plane_normal_z) < plane_distance_z) {
        discard;
    }
    
    // 3. Renderização Visual Base PBR
    vec4 albedo_tex = texture(texture_albedo, UV);
    ALBEDO = albedo_color.rgb * albedo_tex.rgb;
    METALLIC = metallic;
    ROUGHNESS = roughness;
    EMISSION = emission_color.rgb;
    
    // 4. Colorize as faces internas (Opcional para dar "tampa" falsa ao buraco cortado)
    if (!FRONT_FACING) {
        ALBEDO = albedo_color.rgb * 0.5; // Escurece as faces internas do corte para distinguir da casca
    }
}
