#!/usr/bin/env bash
# Creates the topics the RocketMQ tests need, once the broker has registered with the nameserver
# (docker-compose-rocketmq.yaml's create-topic service waits for that before running this). RocketMQ
# 5 does not auto-create topics through the gRPC proxy, so every topic a test publishes to has to
# exist before the tests run.
#
# Each mqadmin call starts its own JVM and takes about four seconds, so the 112 topics took over
# eight minutes one after another. They are independent of each other, so they are created
# TOPIC_CREATE_PARALLELISM at a time instead.
set -euo pipefail

NAMESRV="${NAMESRV:-nameserver:9876}"
PARALLELISM="${TOPIC_CREATE_PARALLELISM:-8}"
MQADMIN=$(ls -d /home/rocketmq/rocketmq-*/bin/mqadmin | head -1)

# One "<topic> <message.type>" pair per line.
TOPICS="
bt_building_dispatch NORMAL
bt_building_dispatch_async NORMAL
bt_named_gateway_dispatch NORMAL
bt_mc_cloudevents NORMAL
bt_mc_cloudevents_async NORMAL
rmq_dlq_source NORMAL
rmq_dlq_target NORMAL
rmq_dlq_invalid NORMAL
orders NORMAL
orders-dlq NORMAL
orders-invalid NORMAL

gen_r_post_msg NORMAL
gen_r_multi_msg NORMAL
gen_r_multi_thread NORMAL
gen_r_activity NORMAL
gen_r_requeue NORMAL
gen_r_dlq NORMAL
gen_r_dlq_target NORMAL
gen_r_requeue_delay DELAY
gen_r_delayed_msg DELAY
gen_r_partition_key FIFO

gen_p_post_msg NORMAL
gen_p_multi_msg NORMAL
gen_p_multi_thread NORMAL
gen_p_activity NORMAL
gen_p_requeue NORMAL
gen_p_dlq NORMAL
gen_p_dlq_target NORMAL
gen_p_requeue_delay DELAY
gen_p_delayed_msg DELAY
gen_p_partition_key FIFO

gen_r_exhaust NORMAL
gen_r_exhaust_DLQ NORMAL
gen_r_exhaust_Invalid NORMAL
gen_p_exhaust NORMAL
gen_p_exhaust_DLQ NORMAL
gen_p_exhaust_Invalid NORMAL

gen_r_send_delayed_msg DELAY
gen_r_rq_redeliver NORMAL
gen_r_rq_again NORMAL
gen_r_rq_delay_again DELAY
gen_r_rq_zero NORMAL
gen_r_rq_after NORMAL
gen_r_nack NORMAL
gen_r_nack2 NORMAL
gen_r_rej_de NORMAL
gen_r_rej_de_DLQ NORMAL
gen_r_rej_meta NORMAL
gen_r_rej_meta_DLQ NORMAL
gen_r_rej_noch NORMAL
gen_r_rej_fb NORMAL
gen_r_rej_fb_DLQ NORMAL
gen_r_rej_inv NORMAL
gen_r_rej_inv_DLQ NORMAL
gen_r_rej_inv_Invalid NORMAL
gen_r_rej_unk NORMAL
gen_r_rej_unk_DLQ NORMAL
gen_r_rej_unk_Invalid NORMAL

gen_p_send_delayed_msg DELAY
gen_p_rq_redeliver NORMAL
gen_p_rq_again NORMAL
gen_p_rq_delay_again DELAY
gen_p_rq_zero NORMAL
gen_p_rq_after NORMAL
gen_p_nack NORMAL
gen_p_nack2 NORMAL
gen_p_rej_de NORMAL
gen_p_rej_de_DLQ NORMAL
gen_p_rej_meta NORMAL
gen_p_rej_meta_DLQ NORMAL
gen_p_rej_noch NORMAL
gen_p_rej_fb NORMAL
gen_p_rej_fb_DLQ NORMAL
gen_p_rej_inv NORMAL
gen_p_rej_inv_DLQ NORMAL
gen_p_rej_inv_Invalid NORMAL
gen_p_rej_unk NORMAL
gen_p_rej_unk_DLQ NORMAL
gen_p_rej_unk_Invalid NORMAL

rmq_measure_delivery_attempt NORMAL

rmq_rej_none_r NORMAL
rmq_rej_none_r_DLQ NORMAL
rmq_rej_none_p NORMAL
rmq_rej_none_p_DLQ NORMAL
rmq_stale_hc_r NORMAL
rmq_stale_hc_p NORMAL
rmq_budget_m1_r NORMAL
rmq_budget_m1_r_DLQ NORMAL
rmq_budget_m1_p NORMAL
rmq_budget_m1_p_DLQ NORMAL
rmq_budget_first_r NORMAL
rmq_budget_first_r_DLQ NORMAL
rmq_budget_first_p NORMAL
rmq_budget_first_p_DLQ NORMAL
rmq_delivery_count_r NORMAL
rmq_delivery_count_p NORMAL
rmq_lease_lapse_r NORMAL
rmq_lease_lapse_p NORMAL
rmq_dlq_count_r NORMAL
rmq_dlq_count_r_DLQ NORMAL
rmq_dlq_count_p NORMAL
rmq_dlq_count_p_DLQ NORMAL
rmq_requeue_beyond_lease_r NORMAL
rmq_requeue_beyond_lease_p NORMAL
rmq_nack_r NORMAL
rmq_nack_p NORMAL
rmq_requeue_over_max_r NORMAL
rmq_requeue_over_max_p NORMAL
rmq_requeue_negative_r NORMAL
rmq_requeue_negative_p NORMAL
rmq_broker_call_fails_r NORMAL
rmq_broker_call_fails_p NORMAL
"

echo "Creating topics, ${PARALLELISM} at a time..."
export MQADMIN NAMESRV
# xargs exits non-zero if any updateTopic fails, which fails this script and the container.
grep -v '^$' <<< "$TOPICS" | xargs -P "$PARALLELISM" -L 1 bash -c '
  if "$MQADMIN" updateTopic -n "$NAMESRV" -t "$0" -c DefaultCluster -r 1 -w 1 -a "+message.type=$1" > /dev/null; then
    echo "created $0 ($1)"
  else
    echo "FAILED to create $0 ($1)" >&2
    exit 1
  fi
'

echo 'done'
