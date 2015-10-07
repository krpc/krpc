local root = vessel.parts.root
local stack = {{root, 0}}
while #stack > 0 do
  local part,depth = unpack(table.remove(stack))
  local attach_mode
  if part.axially_attached then
    attach_mode = 'axial'
  else -- radially_attached
    attach_mode = 'radial'
  end
  print(string.rep(' ', depth) .. part.title .. ' - ' .. attach_mode)
  for _,child in ipairs(part.children) do
    table.insert(stack, {child, depth+1})
  end
end
