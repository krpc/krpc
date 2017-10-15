#include "krpc/stream_impl.hpp"

#include "krpc/client.hpp"
#include "krpc/error.hpp"
#include "krpc/services/krpc.hpp"

namespace krpc {

StreamImpl::StreamImpl(Client * client, google::protobuf::uint64 id,
                       std::recursive_mutex * update_lock) :
  client(client),
  id(id),
  update_lock(update_lock),
  started(false),
  updated(false),
  condition_lock(condition_mutex, std::defer_lock) {
}

Client * StreamImpl::get_client() const {
  return client;
}

google::protobuf::uint64 StreamImpl::get_id() const {
  return id;
}

void StreamImpl::start() {
  if (!started) {
    services::KRPC(client).start_stream(id);
    started = true;
  }
}

bool StreamImpl::has_started() const {
  return started;
}

const std::string& StreamImpl::get_data() {
  if (!updated)
    throw StreamError("Stream has no value");
  if (exception)
    std::rethrow_exception(exception);
  return data;
}

void StreamImpl::update(const std::string& data, const std::exception_ptr& exception) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  updated = true;
  this->data = data;
  this->exception = exception;
}

bool StreamImpl::has_updated() const {
  return updated;
}

std::condition_variable& StreamImpl::get_condition() {
  return condition;
}

std::unique_lock<std::mutex>& StreamImpl::get_condition_lock() {
  return condition_lock;
}

const StreamImpl::Callbacks& StreamImpl::get_callbacks() const {
  return callbacks;
}

void StreamImpl::add_callback(const Callback& callback) {
  std::lock_guard<std::recursive_mutex> guard(*update_lock);
  callbacks.push_back(callback);
}

void StreamImpl::remove() {
  client->remove_stream(id);
}

}  // namespace krpc
