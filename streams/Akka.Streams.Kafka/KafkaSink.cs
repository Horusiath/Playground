using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Streams.Dsl;

namespace Akka.Streams.Kafka
{

    #region messages

    /// <summary>
    /// Input element of <see cref="KafkaSink.Committable{TKey,TValue}"/> and <see cref="KafkaSink.Flow"/>.
    /// </summary>
    public sealed class KafkaMessage<TKey, TValue, TPassed>
    {
        /// <summary>
        /// Contains a topic name to which the record is being sent, an optional
        /// partition number, and an optional key and valsue.
        /// </summary>
        public readonly RdKafka.Message Record;

        /// <summary>
        /// Holds any element that is passed through the <see cref="KafkaSink.Flow"/>
        /// and included in the <see cref="KafkaResult{TKey,TValue,TPassed}"/>. That is useful when some context 
        /// is needed to be passed on downstream operations. That could be done with unzip/zip, but this is more convenient.
        /// It can for example be a <see cref="ICommittableOffset"/> or <see cref="ICommittableOffsetBatch"/>
        /// that can be committed later in the flow.
        /// </summary>
        public readonly TPassed Passed;

        public KafkaMessage(RdKafka.Message record, TPassed passed = default(TPassed))
        {
            Record = record;
            Passed = passed;
        }
    }

    /// <summary>
    /// Output element of <see cref="KafkaSink.Flow"/>. Emitted when the message has been
    /// successfully published. Includes the original message, metadata returned from KafkaProducer and the
    /// <see cref="Offset"/> of the produced message.
    /// </summary>
    public sealed class KafkaResult<TKey, TValue, TPassed>
    {
        public readonly object Metadata;
        public readonly KafkaMessage<TKey, TValue, TPassed> Message;

        public KafkaResult(object metadata, KafkaMessage<TKey, TValue, TPassed> message)
        {
            Metadata = metadata;
            Message = message;
        }

        public long Offset => Metadata.Offset;
    }

    #endregion

    public sealed class SinkSettings<TKey, TValue>
    {
        
    }

    public static class KafkaSink
    {
        public static Source<RdKafka.Message, Task> Standard<TKey, TValue>(
            SinkSettings<TKey, TValue> settings, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public static Source<KafkaMessage<TKey, TValue, ICommittable>, Task> Committable<TKey, TValue>(
            SinkSettings<TKey, TValue> settings, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public static Flow<KafkaMessage<TKey, TValue, TPass>, KafkaResult<TKey, TValue, TPass>, NotUsed> Flow<TKey, TValue, TPass>(
            SinkSettings<TKey, TValue> settings)
        {
            throw new NotImplementedException();
        }
    } 
}