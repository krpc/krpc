#pragma once

#include <google/protobuf/stubs/port.h>

#include <condition_variable>  // NOLINT(build/c++11)
#include <exception>
#include <functional>
#include <mutex>  // NOLINT(build/c++11)
#include <string>
#include <vector>

namespace krpc {

class Client;

class StreamImpl {
 public:
  StreamImpl(Client * client, google::protobuf::uint64 id, std::recursive_mutex * update_lock);
  Client * get_client() const;
  google::protobuf::uint64 get_id() const;
  void start();
  bool has_started() const;
  const std::string& get_data();
  void update(const std::string& data, const std::exception_ptr& exception);
  bool has_updated() const;
  std::condition_variable& get_condition();
  std::unique_lock<std::mutex>& get_condition_lock();
  typedef std::function<void(const std::string&)> Callback;
  typedef std::vector<Callback> Callbacks;
  const Callbacks& get_callbacks() const;
  void add_callback(const Callback& callback);
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
};

}  // namespace krpc
