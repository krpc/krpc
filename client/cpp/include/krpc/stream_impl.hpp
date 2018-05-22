#pragma once

#include <google/protobuf/stubs/port.h>

#include <condition_variable>  // NOLINT(build/c++11)
#include <exception>
#include <functional>
#include <map>
#include <mutex>  // NOLINT(build/c++11)
#include <string>

namespace krpc {

class Client;

class StreamImpl {
 public:
  StreamImpl(Client * client, google::protobuf::uint64 id, std::recursive_mutex * update_lock);
  Client * get_client() const;
  google::protobuf::uint64 get_id() const;
  void start();
  bool has_started() const;
  float rate() const;
  void set_rate(float value);
  const std::string& get_data();
  void update(const std::string& data, const std::exception_ptr& exception);
  bool has_updated() const;
  std::condition_variable& get_condition();
  std::unique_lock<std::mutex>& get_condition_lock();
  typedef std::function<void(const std::string&)> Callback;
  typedef std::map<int, Callback> Callbacks;
  const Callbacks& get_callbacks() const;
  int add_callback(const Callback& callback);
  void remove_callback(int tag);
  void remove();

 private:
  Client * client;
  google::protobuf::uint64 id;
  std::recursive_mutex * update_lock;
  bool started;
  bool updated;
  std::string data;
  std::exception_ptr exception;
  std::condition_variable condition;
  std::mutex condition_mutex;
  std::unique_lock<std::mutex> condition_lock;
  Callbacks callbacks;
  int next_callback_tag;
  float _rate;
};

}  // namespace krpc
