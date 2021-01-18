cd ..
dotnet run -c Release -w InsertV3BenchmarkOperation -e ${ENDPOINT} -k ${KEY} -t ${THROUGHPUT} -n ${DOCUMENTS} --pl ${PARALLELISM}