local luaunit = require 'luaunit'
local class = require 'pl.class'
local t = require 'krpc.service'.to_snake_case

local TestSnakeCase = class()

function TestSnakeCase:test_examples()
  -- Simple cases
  luaunit.assertEquals('server', t('Server'))
  luaunit.assertEquals('my_server', t('MyServer'))

  -- With numbers
  luaunit.assertEquals('int32_to_string', t('Int32ToString'))
  luaunit.assertEquals('32_to_string', t('32ToString'))
  luaunit.assertEquals('to_int32', t('ToInt32'))

  -- With multiple capitals
  luaunit.assertEquals('https', t('HTTPS'))
  luaunit.assertEquals('http_server', t('HTTPServer'))
  luaunit.assertEquals('my_http_server', t('MyHTTPServer'))
  luaunit.assertEquals('http_server_ssl', t('HTTPServerSSL'))

  -- With underscores
  luaunit.assertEquals('_http_server', t('_HTTPServer'))
  luaunit.assertEquals('http__server', t('HTTP_Server'))
end

function TestSnakeCase:test_non_camel_case_examples()
  luaunit.assertEquals('foobar', t('foobar'))
  luaunit.assertEquals('foo__bar', t('foo_bar'))
  luaunit.assertEquals('_foobar', t('_foobar'))
end

return TestSnakeCase
